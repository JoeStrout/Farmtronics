using System.Collections.Generic;

namespace Farmtronics.M1.Filesystem {
	abstract class Disk {

		public delegate void DiskActivityCallback(bool write);

		public DiskActivityCallback diskActivityCallback = null;

		protected void ShowDiskLight(bool write) {
			if (diskActivityCallback != null) diskActivityCallback(write);
		}

		/// <summary>
		/// Prepare this disk for disuse (unmounting or quit of the app).
		/// </summary>
		public virtual void Close() { }

		/// <summary>
		/// Get a list of files in the given directory (which must end in "/").
		/// Returns just the names (not paths) of files immediately within the
		/// given directory.
		/// </summary>
		public abstract List<string> GetFileNames(string dirPath);

		/// <summary>
		/// Return whether a file or directory exists at the given path.  If so, also set
		/// isDirectory to whether it is a directory.
		/// </summary>
		public virtual bool Exists(string filePath, out bool isDirectory) {
			isDirectory = false;
			var finfo = GetFileInfo(filePath);
			if (finfo == null) return false;
			isDirectory = finfo.isDirectory;
			return true;
		}

		public abstract FileInfo GetFileInfo(string filePath);

		/// <summary>
		/// Return whether a file or directory exists at the given path.
		/// </summary>
		public bool Exists(string filePath) {
			bool nobodyCares;
			return Exists(filePath, out nobodyCares);
		}

		/// <summary>
		/// Read the given text file as a string.
		/// </summary>
		/// <param name="filePath"></param>
		public abstract string ReadText(string filePath);

		/// <summary>
		/// Read the given file as a binary data.
		/// </summary>
		/// <param name="filePath"></param>
		public abstract byte[] ReadBinary(string filePath);

		/// <summary>
		/// Return whether this disk can be written to.
		/// </summary>
		public virtual bool IsWriteable() { return false; }

		/// <summary>
		/// Write the given text to a file.
		/// </summary>
		public virtual void WriteText(string filePath, string text) { }

		/// <summary>
		/// Write the given binary data to a file.
		/// </summary>
		public virtual void WriteBinary(string filePath, byte[] data) { }

		/// <summary>
		/// Delete the given file.
		/// </summary>
		public virtual bool MakeDir(string dirPath, out string errMsg) {
			errMsg = "Disk is read-only";
			return false;
		}

		/// <summary>
		/// Delete the given file.
		/// </summary>
		public virtual bool Delete(string filePath, out string errMsg) {
			errMsg = "Disk is read-only";
			return false;
		}

		public List<string> ReadLines(string filePath) {
			string text = ReadText(filePath);
			if (text == null) return null;
			return new List<string>(text.Split(new string[] { "\r\n", "\n", "\r" },
					System.StringSplitOptions.None));
		}

		public virtual void WriteLines(string filePath, List<string> lines) {
			WriteText(filePath, string.Join("\n", lines.ToArray()));
		}

	}	
}
