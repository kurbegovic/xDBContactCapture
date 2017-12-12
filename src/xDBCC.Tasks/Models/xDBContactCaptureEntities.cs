using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xDBCC.Tasks.Models
{
    public partial class xDBContactCaptureEntities : DbContext
    {
        public xDBContactCaptureEntities(string connectionString)
            : base(connectionString)
        { }
    }
}