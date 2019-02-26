using Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace BLL.Models
{
    public class RouteLevelModel
    {
        public int Level { get; set; }
        public Route Route { get; set; }
        public RouteLevelModel Parent { get; set; }
    }
}
