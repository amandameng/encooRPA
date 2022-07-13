//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    DataTable resultDT = (DataTable)dtRow_ProjectSettings["执行记录数据表"];
    DataRow[] failedRecordsDR = resultDT.Select("执行结果 = false");
    if(failedRecordsDR.Length > 0){
        foreach(DataRow dr in failedRecordsDR){
            string msg = string.Format("平台【{0}】运行出错，错误消息：{1}", dr["module_name"].ToString(), dr["错误消息"].ToString());
            globalErrorMsg = string.IsNullOrEmpty(globalErrorMsg) ? msg : (globalErrorMsg + "<br/> " + msg);
        }
    }
}
//在这里编写您的函数或者类