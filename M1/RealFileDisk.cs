using System.Collections;
using System.Collections.Generic;
using System.IO;
using Farmtronics;

public class RealFileDisk : Disk {

	public bool readOnly;		// if true, this disk is write protected

	string basePath;	// our real (native) base path

	public void Open(string basePath) {
		this.basePath = Path.GetFullPath(basePath);
		//Debug.Log("Set base path to: " + this.basePath);
	}
	
	string NativePath(string path) {
		//Debug.Log("Getting native path for " + path);
		path = path.Replace('/', Path.DirectorySeparatorChar);
		if (path.Length == 0 || path[0] != '/') {
			path = basePath + Path.DirectorySeparatorChar + path;
		}
		//Debug.Log("Expanding " + path);
		path = Path.GetFullPath(path);
		if (!path.StartsWith(basePath)) {
			Debug.Log("Error: expected " + path + " to start with " + basePath);
			throw new System.ArgumentException();
		}
		return path;
	}

	/// <summary>
	/// Get a list of files in the given directory (which must end in "/").
	/// If dirPath is null, then returns ALL files on the disk, in all directories,
	/// with their full paths.  Otherwise, it returns just the names (not paths)
	/// of files immediately within the given directory.
	/// </summary>
	public override List<string> GetFileNames(string dirPath) {
		//Debug.Log("GetFileNames(" + dirPath + ")");
		ShowDiskLight(false);
		var names = new List<string>(Directory.GetFileSystemEntries(NativePath(dirPath)));
		for (int i=0; i<names.Count; i++) {
			names[i] = Path.GetFileName(names[i]);
		}
		return names;
	}
	
	/// <summary>
	/// Return whether a file or directory exists at the given path.  If so, also set
	/// isDirectory to whether it is a directory.
	/// </summary>
	public override bool Exists(string filePath, out bool isDirectory) {
		isDirectory = false;
		var finfo = GetFileInfo(filePath);
		if (finfo == null) return false;
		isDirectory = finfo.isDirectory;
		return true;
	}
	
	public override FileInfo GetFileInfo(string filePath) {
		ShowDiskLight(false);
		filePath = NativePath(filePath);
		
		System.IO.FileAttributes attr;
		try {
			attr = System.IO.File.GetAttributes(filePath);			
		} catch {
			return null;
		}
		
		/*
		Here lie some failed attempts at discovering the true name of the file, with
		whatever case is stored on disk.  References on SO claim that you can use 
		Directory.GetFiles or DirectoryInfo.GetFileSystemInfos to search for your file,
		but for me these return 0 results when the case is wrong.  And they're surely
		iterating over all the files in the directory anyway.  So if I really want to
		do this, I will need to just search the directory myself.
		
		string dirPath = Path.GetDirectoryName(filePath);
		string fileName = Path.GetFileName(filePath);
		var trueNames = Directory.GetFiles(dirPath, fileName);
		Debug.Log($"searching for {fileName} in {dirPath} returned {trueNames.Length} true names");
		if (trueNames.Length == 1) fileName = Path.GetFileName(trueNames[0]);
		else if (trueNames.Length > 1) {
			int besti = 0;
			for (int i=0; i<trueNames.Length; i++) {
				if (trueNames[i] == fileName) besti = i;
			}
			fileName = Path.GetFileName(trueNames[besti]);
		}
		Debug.Log($"picked: {fileName}");
				
		var di = new DirectoryInfo(dirPath);
		var infos = di.GetFileSystemInfos(fileName);
		Debug.Log($"GetFileSystemInfos returned {infos.Length} infos");
		*/
		
		var result = new Disk.FileInfo();
		result.isDirectory = (attr & FileAttributes.Directory) > 0;
		if (result.isDirectory) {
			var info = new System.IO.DirectoryInfo(filePath);
			result.date = info.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss");			
		} else {
			var info = new System.IO.FileInfo(filePath);
			result.size = info.Length;
			result.date = info.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss");
		}
		
		return result;
	}
	
	/// <summary>
	/// Read the given text file as a string.
	/// </summary>
	/// <param name="filePath"></param>
	public override string ReadText(string filePath) {
		ShowDiskLight(false);
		try {
			return File.ReadAllText(NativePath(filePath));			
		} catch {
			return null;
		}
	}
	
	/// <summary>
	/// Read the given file as a binary data.
	/// </summary>
	/// <param name="filePath"></param>
	public override byte[] ReadBinary(string filePath) {
		ShowDiskLight(false);
		return File.ReadAllBytes(NativePath(filePath));
	}
	
	/// <summary>
	/// Return whether this disk can be written to.
	/// </summary>
	public override bool IsWriteable() { return !readOnly; }
	
	/// <summary>
	/// Write the given text to a file.
	/// </summary>
	public override void WriteText(string filePath, string text) {
		if (readOnly) return;
		ShowDiskLight(true);
		File.WriteAllText(NativePath(filePath), text);
	}
	
	/// <summary>
	/// Write the given binary data to a file.
	/// </summary>
	public override void WriteBinary(string filePath, byte[] data) {
		if (readOnly) return;
		ShowDiskLight(true);
		File.WriteAllBytes(NativePath(filePath), data);
	}

	/// <summary>
	/// Delete the given file.
	/// </summary>
	public override bool MakeDir(string dirPath, out string errMsg) {
		if (readOnly) {
			errMsg = "disk not writeable";
			return false;
		}
		ShowDiskLight(true);
		dirPath = NativePath(dirPath);
		try {
			Directory.CreateDirectory(dirPath);
			errMsg = null;
			return true;
		} catch (System.Exception e) {
			errMsg = e.Message;
			return false;
		}
	}
	
	/// <summary>
	/// Delete the given file.
	/// </summary>
	public override bool Delete(string filePath, out string errMsg) {
		if (readOnly) {
			errMsg = "disk not writeable";
			return false;
		}
		ShowDiskLight(true);
		FileInfo info = GetFileInfo(filePath);
		if (info == null) {
			errMsg = "File not found";
			return false;
		}
		try {
			if (info.isDirectory) Directory.Delete(NativePath(filePath));
			else File.Delete(NativePath(filePath));
			errMsg = null;
			return true;
		} catch (System.Exception e) {
			errMsg = e.Message;
			return false;
		}
	}
}
