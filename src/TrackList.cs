using DAAP;
using System;
using System.Collections;
using System.Collections.Generic;

namespace MusicBeePlugin
{
    using System.Diagnostics;
    using static Plugin;

    public class TrackList : IPlaylist
    {
        private static MetaDataType[] tags = {
            MetaDataType.Artist,
            MetaDataType.Album,
            MetaDataType.TrackTitle,
            MetaDataType.Genre,
            MetaDataType.Year,
            MetaDataType.TrackNo,
            MetaDataType.TrackCount,
            MetaDataType.Artwork,
            MetaDataType.Composer,
            MetaDataType.Grouping,
            (MetaDataType)FilePropertyType.Duration,
            (MetaDataType)FilePropertyType.Size,
            (MetaDataType)FilePropertyType.Bitrate,
            (MetaDataType)FilePropertyType.DateAdded,
            (MetaDataType)FilePropertyType.DateModified,
            MetaDataType.AlbumArtist,
            MetaDataType.DiscNo,
            MetaDataType.DiscCount,
            MetaDataType.Rating
        };

        const int ARTIST = 0;
        const int ALBUM = 1;
        const int TRACK_TITLE = 2;
        const int GENRE = 3;
        const int YEAR = 4;
        const int TRACK_NO = 5;
        const int TRACK_COUNT = 6;
        const int ARTWORK = 7;
        const int COMPOSER = 8;
        const int GROUPING = 9;
        const int DURATION = 10;
        const int SIZE = 11;
        const int BIT_RATE = 12;
        const int DATE_ADDED = 13;
        const int DATE_MODIFIED = 14;
        const int ALBUM_ARTIST = 15;
        const int DISC_NO = 16;
        const int DISC_COUNT = 17;
        const int RATING = 18;

        private string[] files;
        private Dictionary<string, int> ids = new Dictionary<string, int>();
        private int idCounter = 1;
        private MusicBeeRevisionManager revisionManager;


        public string[] Files { get { return files; } }

        public TrackList()
        {
            Reset();
        }

        private void Add(string filename)
        {
            int existingId;
            if (ids.TryGetValue(filename, out existingId) == false) {
                ids.Add(filename, idCounter++);
            }
        }

        public void Reset()
        {
            if (mbApi.Library_QueryFilesEx("", ref files)) {
                foreach(var file in files) {
                    Add(file);
                }
            } else {
                throw new Exception("Unable to get MusicBee library");
            }
        }

        public void Update(string[] added)
        {
            string[] newFiles = { };
            if (mbApi.Library_QueryFilesEx("", ref newFiles)) {
                foreach(var file in added) {
                    Add(file);
                }

                files = newFiles;
            } else {
                throw new Exception("Unable to get MusicBee library");
            }
        }

        public Track FileToTrack(string file, int id = 0)
        {
            if (id == 0) {
                id = ids[file];
            }

            Track result = null;
            string[] trackTags = { };

            if (mbApi.Library_GetFileTags(file, tags, ref trackTags)) {
                result = new Track();

                result.Artist = trackTags[ARTIST];
                result.Album = trackTags[ALBUM];
                result.AlbumArtist = trackTags[ALBUM_ARTIST];
                result.Title = trackTags[TRACK_TITLE];
                result.Composer = trackTags[COMPOSER];
                result.Grouping = trackTags[GROUPING];
                result.Artwork = trackTags[ARTWORK];

                if (trackTags[GENRE] != null && trackTags[GENRE] != String.Empty) {
                    result.Genre = TrimToCharacter(trackTags[GENRE], ';');
                }

                int year, trackNumber, trackCount, discNumber, discCount;
                if (int.TryParse(trackTags[YEAR], out year)) result.Year = year;
                if (int.TryParse(trackTags[TRACK_NO], out trackNumber)) result.TrackNumber = trackNumber;
                if (int.TryParse(trackTags[TRACK_COUNT], out trackCount)) result.TrackCount = trackCount;
                if (int.TryParse(trackTags[DISC_NO], out discNumber)) result.DiscNumber = discNumber;
                if (int.TryParse(trackTags[DISC_COUNT], out discCount)) result.DiscCount = discCount;

                float rating;
                if (float.TryParse(trackTags[RATING], out rating)) result.Rating = (int)(rating * 20);

                TimeSpan duration;
                if (TimeSpan.TryParseExact(trackTags[DURATION], @"m\:ss", null, out duration)) result.Duration = duration;
                else if (TimeSpan.TryParseExact(trackTags[DURATION], @"h:m\:ss", null, out duration)) result.Duration = duration;

                DateTime added, modified;
                if (DateTime.TryParse(trackTags[DATE_ADDED], out added)) result.DateAdded = added;
                if (DateTime.TryParse(trackTags[DATE_MODIFIED], out modified)) result.DateModified = modified;

                string sizeString = trackTags[SIZE];
                string units = sizeString.Substring(sizeString.Length - 2);
                string sizeValue = sizeString.Substring(0, sizeString.Length - 2);
                double size = 0;
                double.TryParse(sizeValue, out size);
                switch (units) {
                    case "GB": size *= 1024 * 1024 * 1024; break;
                    case "MB": size *= 1024 * 1024; break;
                    case "KB": size *= 1024; break;
                    default: break;
                }

                result.Size = (int)size;

                short bitrate;
                if (short.TryParse(TrimToCharacter(trackTags[BIT_RATE], 'k'), out bitrate)) {
                    result.BitRate = bitrate;
                }

                result.Format = AudioStream.GetFormatFromFileName(file);
                result.SetId(id);
                result.FileName = file;
            }

            return result;
        }

        internal bool TryGetId(string filename, out int id)
        {
            return ids.TryGetValue(filename, out id);
        }

        internal int[] GetIds(string[] filenames)
        {
            int[] result = new int[filenames.Length];

            for (int index = 0; index < result.Length; index++) {
                TryGetId(filenames[index], out result[index]);
            }

            return result;
        }

        internal ContentNode ToNode()
        {
            ArrayList nodes = new ArrayList();

            nodes.Add(new ContentNode("dmap.itemid", 1));
            nodes.Add(new ContentNode("dmap.persistentid", 1));
            nodes.Add(new ContentNode("dmap.itemname", settings.serverName));
            nodes.Add(new ContentNode("dmap.itemcount", files.Length));
            nodes.Add(new ContentNode("daap.baseplaylist", (byte)1));

            return new ContentNode("dmap.listingitem", nodes);
        }

        public ContentNode ToTracksNode(int[] deletedIds)
        {
            ArrayList trackNodes = new ArrayList();

            foreach (var file in files) {
                int id;
                if (TryGetId(file, out id)) {
                    trackNodes.Add(Track.CreatePlaylistNode(id, id));
                } else {
                    Debug.WriteLine(String.Format("Failed to get id for {0}", file));
                }
            }

            ArrayList deletedNodes = null;
            if (deletedIds.Length > 0) {
                deletedNodes = new ArrayList();

                foreach (int id in deletedIds) {
                    deletedNodes.Add(new ContentNode("dmap.itemid", id));
                }
            }

            ArrayList children = new ArrayList();
            children.Add(new ContentNode("dmap.status", 200));
            children.Add(new ContentNode("dmap.updatetype", deletedNodes == null ? (byte)0 : (byte)1));
            children.Add(new ContentNode("dmap.specifiedtotalcount", files.Length));
            children.Add(new ContentNode("dmap.returnedcount", files.Length));
            children.Add(new ContentNode("dmap.listing", trackNodes));

            if (deletedNodes != null)
                children.Add(new ContentNode("dmap.deletedidlisting", deletedNodes));


            return new ContentNode("daap.playlistsongs", children);
        }
        
        internal MusicBeeRevisionManager RevisionManager
        {
            get { return revisionManager; }
            set { revisionManager = value; }
        }

        internal Track LookupTrackById(int id)
        {
            Track result = null;
            
            foreach(KeyValuePair<string, int> file in ids) {
                if (file.Value == id) {
                    result = FileToTrack(file.Key);
                    break;
                }
            }

            return result;
        }

        public int Count
        {
            get
            {
                return files.Length;
            }
        }
        
        public IEnumerator<Track> GetEnumerator()
        {
            foreach (var file in files) {
                yield return FileToTrack(file);
            }

            yield break;
        }
        
        public int Id { get { return 1; } }
        
        public int[] GetDeletedIds(int revision)
        {
            return revisionManager.GetDeletedIds(revision);
        }        
    }
}