using Firebase.Storage;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Auth.Services
{
    public class StorageService
    {
        private readonly string _bucketName = "streamline-4994a.firebasestorage.app";

        // ------------------------------------------
        // Name: UploadFileAsync
        // Purpose: Upload any file stream to Firebase Storage
        // Re-use: Used by controllers to upload user documents
        // Input Parameters: fileStream (Stream), folderName (string), fileName (string)
        // Output Type: Task<string> (public download URL)
        // ------------------------------------------
        public async Task<string> UploadFileAsync(Stream fileStream, string folderName, string fileName)
        {
            var cancellation = new CancellationTokenSource();
            var storage = new FirebaseStorage(_bucketName);

            var uploadTask = storage
                .Child(folderName)
                .Child(fileName)
                .PutAsync(fileStream, cancellation.Token);

            // returns the file’s public URL
            return await uploadTask;
        }

        // ------------------------------------------
        // Name: DeleteFileAsync
        // Purpose: Delete a file from Firebase Storage
        // Re-use: Used when a qualification or record is deleted
        // Input Parameters: folderName (string), fileName (string)
        // Output Type: Task
        // ------------------------------------------
        public async Task DeleteFileAsync(string folderName, string fileName)
        {
            var storage = new FirebaseStorage(_bucketName);
            var file = storage.Child(folderName).Child(fileName);
            await file.DeleteAsync();
        }
    }
}
