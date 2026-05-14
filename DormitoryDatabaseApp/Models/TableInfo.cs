using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace DormitoryDatabaseApp.Models
{
    public class TableInfo
    {
        public string DisplayName { get; set; } = string.Empty;
        public string TableName { get; set; } = string.Empty;

        public override string ToString()
        {
            return DisplayName;
        }
    }
}

