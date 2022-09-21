namespace Farmtronics.M1.Filesystem {
	class FileInfo {
			public string date;         // file timestamp, in SQL format
			public long size;           // size in bytes
			public bool isDirectory;    // true if it's a directory
			public string comment;      // file comment
		}
}