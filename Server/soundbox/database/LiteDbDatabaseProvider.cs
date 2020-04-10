using LiteDB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Soundbox
{
    /// <summary>
    /// Singleton <see cref="IDatabaseProvider"/> that uses the NoSQL <see cref="LiteDB"/> database.<br/>
    /// The file tree is implemented on a single "collection" (<see cref="LiteDB.ILiteCollection{T}"/>)
    /// containing both <see cref="Sound"/>s and <see cref="SoundboxDirectory"/>s.
    /// Thus that collection references itself twice via <see cref="SoundboxNode.ParentDirectory"/> and <see cref="SoundboxDirectory.Children"/>.
    /// </summary>
    public class LiteDbDatabaseProvider : IDatabaseProvider, IDisposable
    {
        private const string COLLECTION_SOUNDS_NAME = "sounds";

        protected readonly LiteDatabase Database;
        protected readonly BsonMapper BsonMapper;

        public LiteDbDatabaseProvider(ISoundboxConfigProvider config)
        {
            //prepare the bson document mapper
            BsonMapper = new BsonMapper();
            BsonMapper.IncludeFields = true;
            BsonMapper.Entity<SoundboxNode>()
                .Id(f => f.ID, false);

            BsonMapper.Entity<SoundboxDirectory>()
                .DbRef(f => f.Children, COLLECTION_SOUNDS_NAME)
                .DbRef(f => f.ParentDirectory, COLLECTION_SOUNDS_NAME);

            BsonMapper.Entity<Sound>()
                .DbRef(f => f.ParentDirectory, COLLECTION_SOUNDS_NAME);

            //open the database file
            var databaseDirectory = config.GetRootDirectory() + "database/";
            Directory.CreateDirectory(databaseDirectory);
            Database = new LiteDatabase(databaseDirectory + "lite_sounds.db", BsonMapper);
        }

        public void Dispose()
        {
            if(Database != null)
            {
                Database.Dispose();
            }
        }

        protected ILiteCollection<SoundboxNode> GetSoundsCollection()
        {
            return Database.GetCollection<SoundboxNode>(COLLECTION_SOUNDS_NAME);
        }

        public Task<SoundboxDirectory> Get()
        {
            var collection = GetSoundsCollection();

            //first load the root directory along with its children
            var resultRoot = collection.Query()
                .Include(BsonExpression.Create("children"))
                .Where(f => f.ParentDirectory == null);
            if (!resultRoot.Exists())
                return Task.FromResult((SoundboxDirectory) null);

            var root = resultRoot.FirstOrDefault() as SoundboxDirectory;
            if (root == null)
                return Task.FromResult((SoundboxDirectory)null);

            //recursively load its child directories
            LoadDirectoryChildren(collection, root);

            return Task.FromResult(root);
        }

        /// <summary>
        /// Iterates through all of the directory's <see cref="SoundboxDirectory.Children"/>
        /// and loads the content of child directories via <see cref="LoadDirectory(ILiteCollection{SoundboxNode}, SoundboxDirectory)"/>.
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="directory"></param>
        private void LoadDirectoryChildren(ILiteCollection<SoundboxNode> collection, SoundboxDirectory directory)
        {
            foreach (var child in directory.Children)
            {
                child.ParentDirectory = directory;
                if (child is SoundboxDirectory childDirectory)
                {
                    LoadDirectory(collection, childDirectory);
                }
            }
        }

        /// <summary>
        /// Recursively loads the entire directory's content.
        /// The given directory itself is loaded and modified as required (in/out parameter).
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="directory"></param>
        private void LoadDirectory(ILiteCollection<SoundboxNode> collection, SoundboxDirectory directory)
        {
            //at this point we already have the children's IDs loaded
            //still it's probably most efficient to just load the directory's children again
            var resultChildren = collection.Query()
                .Include(BsonExpression.Create("children"))
                .Where(f => f.ID == directory.ID)
                .Select(BsonExpression.Create("children"));

            ICollection<SoundboxNode> children = new List<SoundboxNode>();
            foreach(var childRow in resultChildren.ToEnumerable())
            {
                if (childRow == null)
                    continue;

                var bsonChildren = childRow["children"];
                if (bsonChildren == null)
                    continue;

                foreach(var bsonChild in bsonChildren.AsArray)
                {
                    var child = this.BsonMapper.Deserialize<SoundboxNode>(bsonChild);
                    //test for dangling references:
                    if(child.ID == default)
                    {
                        //not a proper file
                        continue;
                    }

                    children.Add(child);
                }
            }

            directory.Children = children;

            //continue loading
            LoadDirectoryChildren(collection, directory);
        }

        public Task Insert(SoundboxNode file)
        {
            GetSoundsCollection().Insert(file);
            //TODO interface commit
            Database.Checkpoint();
            return Task.FromResult(true);
        }

        public Task Update(SoundboxNode file)
        {
            GetSoundsCollection().Update(file);
            //TODO interface commit
            Database.Checkpoint();
            return Task.FromResult(true);
        }

        public Task Delete(SoundboxNode file)
        {
            DeleteRecursive(GetSoundsCollection(), file);
            //TODO interface commit
            Database.Checkpoint();
            return Task.FromResult(true);
        }

        /// <summary>
        /// Deletes the given file and all its descendants if it is a <see cref="SoundboxDirectory"/>.
        /// </summary>
        /// <param name="file"></param>
        protected void DeleteRecursive(ILiteCollection<SoundboxNode> collection, SoundboxNode file)
        {
            if(file is SoundboxDirectory directory)
            {
                //is directory: recursively delete all content
                foreach (var child in directory.Children)
                {
                    DeleteRecursive(collection, child);
                }
            }

            collection.Delete(file.ID);
        }
    }
}
