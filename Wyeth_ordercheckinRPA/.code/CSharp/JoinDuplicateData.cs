//代码执行入口，请勿修改或删除
public void Run()
{
    foreach(DataRow dupRow in duplicateTable.Rows)
    {
        bool existAcc = false;
        foreach(DataRow accRow in TableByAccount.Rows)
        {
            if (dupRow[0].ToString().Equals(accRow["Account"].ToString()))
            {
                existAcc = true;
                DataTable inTable = (DataTable)accRow["Table"];
                List<object> oL = dupRow.ItemArray.ToList();
                oL.RemoveAt(0);
                oL.RemoveAt(0);
                oL.Add("");
                oL.Add("失败");
                oL.Add("重复订单");
                inTable.Rows.Add(oL.ToArray());
            }
        }
        if (!existAcc)
        {
            DataRow accRow = TableByAccount.NewRow();
            
            DataTable inTable = new DataTable();
            inTable.Columns.Add("Pay");
            inTable.Columns.Add("ReadDate");
            inTable.Columns.Add("soldto");
            inTable.Columns.Add("shipto");
            inTable.Columns.Add("customerName");
            inTable.Columns.Add("POID");
            inTable.Columns.Add("itemName");
            inTable.Columns.Add("Nums");
            inTable.Columns.Add("Amount");
            inTable.Columns.Add("Result");
            inTable.Columns.Add("Reson");
            inTable.Columns.Add("DmsPo");
            inTable.Columns.Add("RDD");
            inTable.Columns.Add("isCon");
            
            accRow["Account"] = dupRow[0];
            accRow["Password"] = "";
            //DataTable inTable = ((DataTable)TableByAccount.Rows[0]["Table"]).Clone();//
            Console.WriteLine("Ia: {0}", string.Join("，", dupRow.ItemArray));
            foreach(DataColumn col in dupRow.Table.Columns){
                Console.WriteLine("col: {0}", col.ColumnName);
                Console.WriteLine("value: {0}", dupRow[col.ColumnName]);
            }
            List<object> oL = dupRow.ItemArray.ToList();
            oL.RemoveAt(0);
            oL.RemoveAt(0);
            oL.Add("");
            oL.Add("失败");
            oL.Add("重复订单*");
            Console.WriteLine("oL: {0}", string.Join("，", oL));
             Console.WriteLine(inTable.Columns.Count);
            inTable.Rows.Add(oL.ToArray());
            accRow["Table"] = inTable;
            TableByAccount.Rows.Add(accRow);
        }
    }
}
//在这里编写您的函数或者类