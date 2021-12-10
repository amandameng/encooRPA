//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    foreach(DataRow dr in 异常订单模板数据表.Rows){
        if(dr["问题分类"].ToString()=="展示用"){
            dr["问题分类"] = string.Empty;
        }
    }
}
//在这里编写您的函数或者类