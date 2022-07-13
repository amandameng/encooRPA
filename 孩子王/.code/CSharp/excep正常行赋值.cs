//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    新1行row["雀巢产品码"]=雀巢筛选后dt.Rows[0]["Nestle_Material_No"].ToString();
    新1行row["区域"]=雀巢筛选后dt.Rows[0]["Region"].ToString();
    新1行row["产品名称"]=雀巢筛选后dt.Rows[0]["Material_Description"].ToString();
    新1行row["Nestle_BU"]=雀巢筛选后dt.Rows[0]["Nestle_BU"].ToString();
    新1行row["雀巢价格"]=雀巢筛选后dt.Rows[0]["Nestle_NPS"].ToString();
    新1行row["雀巢箱规"]=雀巢筛选后dt.Rows[0]["Nestle_Case_Configuration"].ToString();
}
//在这里编写您的函数或者类