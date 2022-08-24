//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
   htmlData = GetHtmlString(targetDataTable);
}
//在这里编写您的函数或者类

public string GetHtmlString(DataTable dt)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<table cellSpacing='0' cellPadding='0' width ='100%' border='1'>");
            sb.Append("<thead valign='middle'>");
            // columns
            DataColumnCollection columns = dt.Columns;
            
            int columnIndex = 0;
            foreach (DataColumn column in columns)
            {
                sb.Append(String.Format("<th><b><span>" + column.ColumnName + "</span></b></th>"));
                columnIndex +=1;
            }
            sb.Append("</thead>");
            int iColsCount = dt.Columns.Count;
            int rowsCount = dt.Rows.Count - 1;
            //string[] highlightedHeader = new String[]{"SKU NO", "SKU List", 行背景基于列名, "产地", "产品属性"};
            
            for (int j = 0; j <= rowsCount; j++)
            {
                sb.Append("<tr>");
                //string 产品分类 = dt.Rows[j][行背景基于列名].ToString();

                for (int k = 0; k <= iColsCount - 1; k++)
                {
                    string tdClassName = "";

                    sb.Append(String.Format("<td {0}>", tdClassName));
                    object obj = dt.Rows[j][k];
                    if (obj == DBNull.Value)
                    {
                        obj = "&nbsp;";//如果是NULL则在HTML里面使用一个空格替换之
                    }
                    if (obj.ToString() == "")
                    {
                        obj = "&nbsp;";
                    }
                    string strCellContent = obj.ToString().Trim();
                    sb.Append("<span>" + strCellContent + "</span>");
                    sb.Append("</td>");
                }
                sb.Append("</tr>");
            }
            sb.Append("</table>");
            return sb.ToString();
        }