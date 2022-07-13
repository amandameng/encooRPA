//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    雀巢nps单价=雀巢筛选后dt.Rows[0]["Nestle_NPS"].ToString();
    箱数=row["整件"].ToString();
    订货单价=row["订货单价"].ToString();
    客户订单金额=row["订货金额"].ToString();
    客户价格=double.Parse(订货单价)*Int32.Parse(比率);
    单价价差=double.Parse(雀巢nps单价)*0.935-(客户价格/1.13*0.935);
    价差=(double.Parse(雀巢nps单价)*double.Parse(箱数)*0.935)-(double.Parse(客户订单金额)/1.13*0.935);
    总价差=总价差+价差;
    订单dt.Rows[行索引]["客户价格"]=客户价格.ToString("N6");
    订单dt.Rows[行索引]["单价价差"]=单价价差.ToString("N6");
    订单dt.Rows[行索引]["价差"]=价差.ToString("N6");
    //订单dt.Rows[行索引]["价差"]=价差.ToString().Split('E')[0].ToString();
}
//在这里编写您的函数或者类