using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Auth.Services
{
    public class DbServices<T> where T : class
    {
        private readonly FirestoreDb _firestoreDb;
        private readonly string _collectionName;
        private readonly CollectionReference _collection;

        public DbServices(string collectionName)
        {
            _firestoreDb = DbFactory.GetFirestoreDb();
            _collectionName = collectionName;
            _collection = _firestoreDb.Collection(collectionName);
        }

        // Get all documents
        public async Task<List<T>> GetAllAsync()
        {
            var snapshot = await _collection.GetSnapshotAsync();
            return snapshot.Documents
                .Select(doc => doc.ConvertTo<T>())
                .ToList();
        }

        // Get by document ID
        public async Task<T?> GetByIdAsync(string id)
        {
            var docRef = _collection.Document(id);
            var snapshot = await docRef.GetSnapshotAsync();
            return snapshot.Exists ? snapshot.ConvertTo<T>() : null;
        }

        // Get by field (string)
        public async Task<T?> GetByFieldAsync(string fieldName, string value)
            => await GetByFieldAsync<string>(fieldName, value);

        // Get by field (int, bool, etc.)
        public async Task<T?> GetByFieldAsync<TValue>(string fieldName, TValue value)
        {
            var query = _collection.WhereEqualTo(fieldName, value).Limit(1);
            var snapshot = await query.GetSnapshotAsync();
            var doc = snapshot.Documents.FirstOrDefault();
            return doc?.ConvertTo<T>();
        }

        // Get all matching a field
        public async Task<List<T>> GetAllWhereAsync(string fieldName, object value)
        {
            var query = _collection.WhereEqualTo(fieldName, value);
            var snapshot = await query.GetSnapshotAsync();
            return snapshot.Documents
                .Select(doc => doc.ConvertTo<T>())
                .ToList();
        }

        // Create with custom ID
        public async Task CreateAsync(string id, T record)
        {
            var docRef = _collection.Document(id);
            await docRef.SetAsync(record);
        }

        // Update entire document
        public async Task UpdateAsync(string id, T record)
        {
            var docRef = _collection.Document(id);
            await docRef.SetAsync(record, SetOptions.Overwrite);
        }

        // Update specific fields only
        public async Task UpdateFieldsAsync(string documentId, Dictionary<string, object> updates)
        {
            var docRef = _collection.Document(documentId);
            await docRef.UpdateAsync(updates);
        }

        // Delete
        public async Task DeleteAsync(string id)
        {
            var docRef = _collection.Document(id);
            await docRef.DeleteAsync();
        }
    }
}