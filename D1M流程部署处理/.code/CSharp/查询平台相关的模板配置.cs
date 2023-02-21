//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
   查询平台Template_SQL = string.Format("SELECT tmp.*, tp.platform_type FROM tb_module_template tmp left join tb_platform tp on tmp.platform_id = tp.id where tp.platform_name = '{0}' ", 平台名称);
    
}
//在这里编写您的函数或者类