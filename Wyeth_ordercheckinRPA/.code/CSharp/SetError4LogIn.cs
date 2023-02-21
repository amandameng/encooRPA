//代码执行入口，请勿修改或删除
public void Run()
{
    DataTable inTable = (DataTable)AccountRow["Table"];
    foreach (DataRow inRow in inTable.Rows)
    {
        inRow["Result"] = "失败";
        inRow["Reson"] = "账户登录失败";
    }
}
