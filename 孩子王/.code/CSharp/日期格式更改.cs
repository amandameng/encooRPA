//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    日期=row["Reqd_Del_Date"].ToString();
    //Console.WriteLine("rdd日期是："+日期);
    生成文件excel_to_order_dt.Rows[行索引]["rdd"]=日期.Substring(0,10).Replace("/","");
}
//在这里编写您的函数或者类