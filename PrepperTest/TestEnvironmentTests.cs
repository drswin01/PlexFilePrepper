namespace PrepperTests
{
    public class TestEnvironmentTests
    {
        private const int FileCount = 100;
        private const string PrepperTestDirectory = "TestFiles";

        [Test]
        public void SetupTestFiles()
        {
            int counter = 1;

            List<string> filePaths = new();
            if (!Directory.Exists(PrepperTestDirectory)) Directory.CreateDirectory(PrepperTestDirectory);
            foreach (string path in Directory.EnumerateFiles(PrepperTestDirectory))
            {
                filePaths.Add(path);
            }
            if (filePaths.Count > 0)
            {
                foreach(string path in filePaths)
                {
                    if (Path.GetFileNameWithoutExtension(path).Equals($"Test{counter}")) continue;
                    if (path == null) throw new NullReferenceException("path was null");
                    string? dir = Path.GetDirectoryName(path);
                    string? ext = Path.GetExtension(path);
                    if (dir != null)
                    {
                        string? newFilePath = Path.Combine(dir, $"Test{counter}{ext}");
                        File.Move(path, newFilePath);
                        counter++;
                    }
                }
            }
            int dif = FileCount - filePaths.Count;
            if (dif <= 0 ) return;
            for (int i = 0; i < dif; i++)
            {
                File.CreateText(Path.Combine(PrepperTestDirectory, $"Test{counter}.txt"));
                counter++;
            }
        }
    }
}
