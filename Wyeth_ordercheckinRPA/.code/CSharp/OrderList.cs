static HashSet<DataTable> tableSetSplited = new HashSet<DataTable>();
public void Run()
{
    Console.WriteLine($"Constraint List Count : {constraint_list.Count()}");
    HashSet<DataView> orderViewSet = new HashSet<DataView>();
    
    HashSet<string> poidList = new HashSet<string>();
    
    /*
    int index = 0;
    foreach(DataRow row in itemTable.Rows)
    {
        row["Result"] = index.ToString();
        index++;
    }
    */
    
    foreach (DataRow row in itemTable.Rows)
    {
        if (row["POID"].ToString() == string.Empty)
        {
            continue;
        }
        if (!poidList.Contains(row["POID"].ToString()))
        {
            poidList.Add(row["POID"].ToString());
            DataView view = new DataView(itemTable);
            view.RowFilter = "convert(POID, 'System.String') = '" + row["POID"].ToString() + "'";
    
            orderViewSet.Add(view);
        }
    }
    
    HashSet<DataTable> orderTableSet = new HashSet<DataTable>();
    
    foreach (var i in orderViewSet)
    {
        DataTable normalDt = itemTable.Clone();
        foreach (DataRow row in i.ToTable().Rows)
        {
        
            if (constraint_list.Contains(row["itemName"].ToString()))
            {
                DataTable dt = itemTable.Clone();
                dt.Rows.Add(row.ItemArray);
                orderTableSet.Add(dt);
            }
            else
            {
                normalDt.Rows.Add(row.ItemArray);
            }
    
        }
        if (normalDt.Rows.Count > 0)
        {
            orderTableSet.Add(normalDt);
        }
    }
    Console.WriteLine("orderTableSet Count : " + orderTableSet.Count());
    tableSetSplited.Clear();
    foreach (DataTable dt in orderTableSet)
    {
        GetDataTableSplit(dt);
    }
    
    itemTable.Clear();
    
    int index = 0;
    foreach(DataTable dt in tableSetSplited)
    {
        foreach (DataRow dr in dt.Rows)
        {
            dr["Result"] = index.ToString();
            index++;
            itemTable.ImportRow(dr);
        }
    }
    
    orderTables = tableSetSplited;
    Console.WriteLine("orderTableSet Count After Split: " + orderTables.Count());
}

        static void GetDataTableSplit(DataTable dt)
        {
            int sum = 0;
            int nowNum = 0;
            int edgeIdx = 0;
            bool isOut = false;
            foreach (DataRow dtRow in dt.Rows)
            {
                nowNum = Convert.ToInt32(dtRow["Nums"]);
                Console.WriteLine($"NowNum : {nowNum}");
                sum += nowNum;
                if (sum > 3060)
                {
                    edgeIdx = dt.Rows.IndexOf(dtRow);
                    isOut = true;
                    break;
                    Console.WriteLine($"Sum : {sum}");
                }
            }

            if (isOut)
            {
                Console.WriteLine($"NOW IN SPLIT");
                DataTable newDataTable = dt.Clone();
                newDataTable.Rows.Clear();
                int newTableIdx = 0;
                for (int i = 0; i < edgeIdx; i++)
                {
                    newDataTable.ImportRow(dt.Rows[i]);
                    newTableIdx++;
                }

                newDataTable.ImportRow(dt.Rows[edgeIdx]);
                newDataTable.Rows[newTableIdx]["Nums"] = 3060 - (sum - nowNum);

                tableSetSplited.Add(newDataTable);
                DataTable newDataTable2 = dt.Clone();
                newDataTable2.Rows.Clear();
                newDataTable2.ImportRow(dt.Rows[edgeIdx]);
                newDataTable2.Rows[0]["Nums"] = sum - 3060;
                for (int i = edgeIdx + 1; i < dt.Rows.Count; i++)
                {
                    newDataTable2.ImportRow(dt.Rows[i]);
                }
                
                GetDataTableSplit(newDataTable2);
            }
            else
            {
                tableSetSplited.Add(dt);
            }
        }