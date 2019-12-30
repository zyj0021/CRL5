/**
* CRL 快速开发框架 V5
* Copyright (c) 2019 Hubro All rights reserved.
* GitHub https://github.com/hubro-xx/CRL5
* 主页 http://www.cnblogs.com/hubro
* 在线文档 http://crl.changqidongli.com/
*/
using LinqToDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestConsole
{
    class LinqToDBContext:DataContext
    {
        public LinqToDBContext()
        {
        }
        public ITable<TestEntity> TestEntitys { get { return this.GetTable<TestEntity>(); } }
    }
}