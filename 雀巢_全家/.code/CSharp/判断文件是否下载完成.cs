//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    if (!orderFileName.Contains("KB"))
    {
        orderFileName = orderFileName.Trim();
        isResultShown = true;
    }
    else
    {
        isResultShown = false;
    }
}
//在这里编写您的函数或者类