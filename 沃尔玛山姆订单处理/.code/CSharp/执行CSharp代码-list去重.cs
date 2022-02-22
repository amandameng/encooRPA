//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    resultList = new List<string>{};
    foreach(string item in sourceList){
        string fileName = System.IO.Path.GetFileName(item);
        int itemCount = resultList.Count;
        for(int i=itemCount-1; i>=0; i--){
            string old_item = resultList[i];
            string oldfileName = System.IO.Path.GetFileName(old_item);
            if(oldfileName == fileName){
                resultList.Remove(old_item);
            }
        }
        resultList.Add(item);
    }
}
//在这里编写您的函数或者类