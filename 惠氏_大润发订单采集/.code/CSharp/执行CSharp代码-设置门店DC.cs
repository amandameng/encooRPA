//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    门店list = new List<string>{};
    DataTable soldToShipToDT = (DataTable)dtRow_ModuleSettings["soldToShipToDT"];
    foreach(DataRow dr in soldToShipToDT.Rows){
        string dc_no = dr["DC编号"].ToString();
        if(!门店list.Contains(dc_no)){
            门店list.Add(dc_no);
        }
    }
}
//在这里编写您的函数或者类