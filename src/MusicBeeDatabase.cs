/*
 * daap-sharp
 * Copyright (C) 2005  James Willcox <snorp@snorp.net>
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 *
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
 */

using System;
using System.Net;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using static MusicBeePlugin.Plugin;
using MusicBeePlugin;

namespace DAAP
{
    public class MusicBeeDatabase
    {

        private static int nextid = 1;
        private int id;
        private string name;

        private TrackList tracks;
        private List<MusicBeePlaylist> playlists = new List<MusicBeePlaylist>();
        
        public int Id
        {
            get { return id; }
        }

        public string[] Files { get { return tracks.Files; } }

        public string Name
        {
            get { return name; }
            set
            {
                name = value;
            }
        }

        public IList<MusicBeePlaylist> Playlists
        {
            get
            {
                return new ReadOnlyCollection<MusicBeePlaylist>(playlists);
            }
        }
        
        private MusicBeeDatabase()
        {
            this.id = nextid++;
        }

        public MusicBeeDatabase(string name) : this()
        {
            this.Name = name;
            this.tracks = mbTracks;

            UpdatePlaylists();
        }
        
        internal void Reset()
        {
            UpdatePlaylists();
            tracks.Reset();
        }

        public void Update(string[] added, string[] removed)
        {
            UpdatePlaylists();
            tracks.Update(added, removed);
        }

        public void UpdatePlaylists()
        {
            mbApi.Playlist_QueryPlaylists();

            string playlistUrl;
            while ((playlistUrl = mbApi.Playlist_QueryGetNextPlaylist()) != null) {
                if (playlists.Find(t => t.Url == playlistUrl) == null) {
                    playlists.Add(new MusicBeePlaylist(playlistUrl));
                }
            }
        }

        public int[] GetIdsOfTracks(string[] filenames)
        {
            return tracks.GetIds(filenames);
        }

        public Track LookupTrackById(int id)
        {
            return tracks.LookupTrackById(id);
        }

        public IPlaylist LookupPlaylistById(int id)
        {
            if (id == 1) {
                return tracks;
            }

            foreach (MusicBeePlaylist pl in playlists)
            {
                if (pl.Id == id) {
                    return pl;
                }
            }

            return null;
        }

        internal int GetIdOfTrack(string file)
        {
            int result = 0;
            tracks.TryGetId(file, out result);
            return result;
        }

        internal ContentNode ToTracksNode(string[] fields, int[] deletedIds)
        {
            ArrayList trackNodes = new ArrayList();

            foreach (Track track in tracks)
            {
                trackNodes.Add(track.ToNode(fields));
            }

            ArrayList deletedNodes = null;
            
            if (deletedIds != null && deletedIds.Length > 0)
            {
                deletedNodes = new ArrayList();

                foreach (int id in deletedIds)
                {
                    deletedNodes.Add(new ContentNode("dmap.itemid", id));
                }
            }

            ArrayList children = new ArrayList();
            children.Add(new ContentNode("dmap.status", 200));
            children.Add(new ContentNode("dmap.updatetype", deletedNodes == null ? (byte)0 : (byte)1));
            children.Add(new ContentNode("dmap.specifiedtotalcount", tracks.Count));
            children.Add(new ContentNode("dmap.returnedcount", tracks.Count));
            children.Add(new ContentNode("dmap.listing", trackNodes));

            if (deletedNodes != null)
            {
                children.Add(new ContentNode("dmap.deletedidlisting", deletedNodes));
            }

            return new ContentNode("daap.databasesongs", children);
        }

        internal ContentNode ToPlaylistsNode()
        {
            ArrayList nodes = new ArrayList();

            ArrayList basePlaylistNode = new ArrayList();
            basePlaylistNode.Add(new ContentNode("dmap.itemid", 1));
            basePlaylistNode.Add(new ContentNode("dmap.persistentid", (long)1));
            basePlaylistNode.Add(new ContentNode("dmap.itemname", name));
            basePlaylistNode.Add(new ContentNode("dmap.itemcount", tracks.Count));
            basePlaylistNode.Add(new ContentNode("daap.baseplaylist", (byte)1));

            nodes.Add(new ContentNode("dmap.listingitem", basePlaylistNode));

            foreach (MusicBeePlaylist pl in playlists)
            {
                nodes.Add(pl.ToNode(false));
            }

            return new ContentNode("daap.databaseplaylists",
                                    new ContentNode("dmap.status", 200),
                                    new ContentNode("dmap.updatetype", (byte)0),
                                    new ContentNode("dmap.specifiedtotalcount", nodes.Count),
                                    new ContentNode("dmap.returnedcount", nodes.Count),
                                    new ContentNode("dmap.listing", nodes));
        }

        internal ContentNode ToDatabaseNode()
        {
            return new ContentNode("dmap.listingitem",
                                    new ContentNode("dmap.itemid", id),
                                    new ContentNode("dmap.persistentid", (long)id),
                                    new ContentNode("dmap.itemname", name),
                                    new ContentNode("dmap.itemcount", tracks.Count),
                                    new ContentNode("dmap.containercount", playlists.Count + 1));
        }


        private bool IsUpdateResponse(ContentNode node)
        {
            return node.Name == "dmap.updateresponse";
        }
    }
}
