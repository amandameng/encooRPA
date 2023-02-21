//代码执行入口，请勿修改或删除
public void Run()
{    
    foreach(DataRow row in order.Rows)
    {
        indexList.Add(row["Result"].ToString());
    }
}
//在这里编写您的函数或者类