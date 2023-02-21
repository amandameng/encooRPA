//代码执行入口，请勿修改或删除
public void Run()
{
   
    foreach (DataRow inRow in inTable.Rows)
    {
        if (indexAmountDic.Keys.Contains(inRow["Result"].ToString()))
        {
            inRow["Amount"] = indexAmountDic[inRow["Result"].ToString()];
        }
        if (indexDmsPoDic.Keys.Contains(inRow["Result"].ToString()))
        {
            inRow["DmsPo"] = indexDmsPoDic[inRow["Result"].ToString()];
        }
    }
    
    foreach (string str in indexList)
    {
        foreach (DataRow inRow in inTable.Rows)
        {
            if (str.Equals(inRow["Result"].ToString()))
            {
                inRow["Result"] = "失败";
                inRow["Reson"] = "RPA系统写入失败";
            }
        }
    }
    
    foreach (DataRow inRow in inTable.Rows)
    {
        if (!inRow["Result"].ToString().Equals("失败"))
        {
            inRow["Result"] = "成功";
        }
        inRow["ReadDate"] = DateTime.Now.ToUniversalTime().ToString();
    }
    
}
//在这里编写您的函数或者类