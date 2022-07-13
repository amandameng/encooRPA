//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    //clean_order_dt.Rows.Clear();
    新1行row=clean_order_dt.NewRow();
    
    新1行row["渠道"]=Nin;
    新1行row["读单日期"]=当前年月日;
    新1行row["客户名称"]=渠道名称;
    新1行row["雀巢PO_No"]=订单号;
    新1行row["客户Po_No"]=订单号;
    新1行row["订单数量"]=整件合计;
    新1行row["区域"]=雀巢dt.Rows[0]["Region"].ToString();
    //新1行row["交货地"]="";
    //新1行row["要求送货日"]="";
    //新1行row["配送时间"]="";
    新1行row["有效期"]=有效日期带横线;
    //新1行row["订单类型"]="";
    //新1行row["档期"]="";
    //新1行row["备注"]="";
    新1行row["created_time"]=当前时间;
    
    clean_order_dt.Rows.Add(新1行row);
}
//在这里编写您的函数或者类