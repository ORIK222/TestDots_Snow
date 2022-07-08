using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Linq;

public static class FileHelper
{
    /// <summary>
    /// Reserves a unique filename for writing data into. As a result creates an empty file, so that subsequent call to this function will create a new reserved file.
    /// </summary>
    /// <param name="directory">The directory.</param>
    /// <param name="fileName">Name of the file.</param>
    /// <param name="extension">The file extension (including the dot symbol).</param>
    /// <returns>
    /// A unique full path.
    /// </returns>
    public static string ReserveUniqueFileName(string directory, string fileName, string extension = null)
    {
        var files = Directory.GetFiles(directory).Select(x => Path.GetFileName(x).ToLower());
        string fileLower = fileName.ToLower();
        string extensionLower = extension?.ToLower();
        string path = fileLower + extensionLower;
        int index = -1;
        // comparison using lowercase only for faster computations.
        while (files.Any(x => x == path))
        {
            path = fileLower + (++index).ToString() + extensionLower;
        }

        string newPath;
        // make nice filename with upper and lowercase letters.
        if (index == -1)
        {
            newPath = Path.Combine(directory, fileName + extension);
        }
        else
        {
            newPath = Path.Combine(directory, fileName + index.ToString() + extension);
        }
        
        // this is for reserving the file at disk.
        File.Create(newPath).Dispose();
        return newPath;
    }

    public class HashData
    {

    }
}
