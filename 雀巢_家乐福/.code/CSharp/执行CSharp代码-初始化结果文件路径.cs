//代码执行入口，请勿修改或删除
public void Run()
{
    eto_file_path = Path.Combine(当前结果文件夹, String.Format("Copy of Excel To Order_{0}.xlsx", timenowStr));
    eto_template_file = Path.Combine(配置文件夹, "Copy of Excel To Order template.xlsx");
    exception_order_file_path = Path.Combine(当前结果文件夹, String.Format("Exception Order_{0}.xlsx", timenowStr));
    exception_order_template_file = Path.Combine(配置文件夹, "Exception Order template.xlsx");   
    clean_order_file_path = Path.Combine(当前结果文件夹, String.Format("Clean Order List_{0}.xlsx", timenowStr));
    clean_order_template_file = Path.Combine(配置文件夹, "Clean Order List template.xlsx");   
}
//在这里编写您的函数或者类