using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;

using DAAP;

namespace MusicBeePlugin {
    using System.Diagnostics;
    using static Plugin;

    public class MusicBeeRevisionManager
    {
        private struct Revision
        {
            public int id;

            public int[] removedIds;
        }

        [Flags]
        public enum NotificationType : int
        {
            None = 0,
            FileAdded = 1,
            FileRemoved = 2,
            FileChanged = 4
        }

        Stack<Revision> revisions = new Stack<Revision>();
        MusicBeeDatabase musicBeeDatabase;

        DateTime lastUpdate = DateTime.Now;
        DateTime lastNotify = DateTime.Now;
        TimeSpan timeToWait = new TimeSpan(0, 0, 1);
        Thread worker;

        int numberOfOpenConnections = 0;
        object updateWaitHandle = new object();

        NotificationType receivedNotificationTypes = NotificationType.None;

        public int Current {
            get { return (revisions.Count == 0) ? 2 : revisions.Peek().id; }
        }

        public MusicBeeRevisionManager(MusicBeeDatabase db)
        {
            this.musicBeeDatabase = db;
            db.tracks.RevisionManager = this;

            ThreadStart start = new ThreadStart(delegate {
                while (true) {
                    lock (worker) {
                        Monitor.Wait(worker);

                        while ((DateTime.Now - lastNotify) < timeToWait) {
                            Monitor.Wait(worker, timeToWait);
                        }

                        Update();
                    }
                }
            });
            
            worker = new Thread(start);
            worker.IsBackground = true;
            worker.Start();
        }

        public void Notify(NotificationType type)
        {
            lock (worker) {
                receivedNotificationTypes |= type;

                lastNotify = DateTime.Now;

                if ((DateTime.Now - lastUpdate) > timeToWait) {
                    Monitor.Pulse(worker);
                }
            }
        }

        public int WaitForUpdate(int clientRevision)
        {
            int currentRevision = Current;

            lock (updateWaitHandle)
            {
                if (clientRevision == currentRevision) {
                    Monitor.Wait(updateWaitHandle);

                    currentRevision = Current;
                }
            }

            return currentRevision;
        }

        private void Update()
        {
            string[] removed = { };
            string[] updated = { };
            string[] added = { };

            lock (updateWaitHandle) {
                Plugin.mbApi.Library_GetSyncDelta(musicBeeDatabase.Files, lastUpdate, LibraryCategory.Music | LibraryCategory.Inbox, ref added, ref updated, ref removed);

                if (receivedNotificationTypes.HasFlag(NotificationType.FileAdded)) {
                    foreach (string file in added) {
                        int addedId = musicBeeDatabase.GetIdOfTrack(file);

                        if (addedId != 0) {
                            // Re-added tracks are zeroed in the revision history
                            foreach (Revision revision in revisions) {
                                for (int index = 0; index < revision.removedIds.Length; index++) {
                                    int removedId = revision.removedIds[index];
                                    if (addedId == removedId) {
                                        revision.removedIds[index] = 0;
                                        goto breakOuter;
                                    }
                                }
                            }
                            breakOuter:;
                        }
                    }
                }

                if (((receivedNotificationTypes & (NotificationType.FileAdded | NotificationType.FileRemoved)) != NotificationType.None) && (added.Length > 0 || removed.Length > 0)) {
                    if (numberOfOpenConnections == 0) {
                        Clear();
                    } else {
                        int nextRevision = Current + 1;

                        revisions.Push(new Revision
                        {
                            id = nextRevision,
                            removedIds = musicBeeDatabase.GetIdsOfTracks(removed)
                        });
                    }

                    musicBeeDatabase.Update(added, removed);

                }

                lastUpdate = DateTime.Now;
                Monitor.PulseAll(updateWaitHandle);
            }

            receivedNotificationTypes = NotificationType.None;
        }

        public int[] GetDeletedIds(int revisionId)
        {
            List<int[]> removedIdSets = new List<int[]>();
            int totalRemovedIds = 0;

            foreach (Revision revision in revisions) {
                removedIdSets.Add(revision.removedIds);
                totalRemovedIds += revision.removedIds.Length;

                if (revision.id == revisionId) {
                    break;
                }
            }

            int index = 0;
            int[] result = new int[totalRemovedIds];
            foreach (int[] removedIdSet in removedIdSets) {
                foreach (int id in removedIdSet) {
                    if (id != 0) {
                        result[index++] = id;
                    }
                }
            }

            return result;
        }

        internal void Stop()
        {
            lock (updateWaitHandle) {
                worker.Abort();
                Monitor.PulseAll(updateWaitHandle);
            }
        }

        private void Clear()
        {
            Debug.Assert(Monitor.IsEntered(updateWaitHandle));

            lastUpdate = DateTime.Now;
            revisions.Clear();
        }

        internal void Reset()
        {
            lock (updateWaitHandle) {
                musicBeeDatabase.Reset();
                Clear();
                Monitor.PulseAll(updateWaitHandle);
            }
        }

        internal void OnLogin(object o, UserArgs args)
        {
            Interlocked.Increment(ref numberOfOpenConnections);
        }

        internal void OnLogout(object o, UserArgs args)
        {
            Interlocked.Decrement(ref numberOfOpenConnections);
        }
    }
}

