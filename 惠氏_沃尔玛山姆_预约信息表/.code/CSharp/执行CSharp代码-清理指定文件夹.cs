//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    cleanDir(endTime, targetDir, keepDays, isFile);
}
//在这里编写您的函数或者类

public void cleanDir(DateTime dt, string dirPath, uint keepDays, bool isFile)
    {
        // 保留文件起始日
        DateTime startKeepDate = dt.AddDays(-keepDays);
        
        if (String.IsNullOrEmpty(dirPath) || !Directory.Exists(dirPath))
        {
            return;
        }
        DirectoryInfo di = new DirectoryInfo(dirPath);
        if (!isFile)
        {
            DirectoryInfo[] arrDi = di.GetDirectories();
            foreach (DirectoryInfo dir in arrDi)
            {
                bool isInvalid = dir.LastWriteTime.CompareTo(startKeepDate) < 0;
                if (isInvalid)
                {
                    Directory.Delete(dir.FullName, true);
                }
            }
        }
        else
        {
            FileInfo[] arrFi = di.GetFiles();
            foreach(FileInfo file in arrFi)
            {
                bool isInvalid = file.LastWriteTime.CompareTo(startKeepDate) < 0;
                if (isInvalid)
                {
                    File.Delete(file.FullName);
                }
            }
        }
        

    }