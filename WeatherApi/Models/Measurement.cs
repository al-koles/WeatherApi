// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace WeatherApi.Models
{
    public partial class Measurement
    {
        public int CityId { get; set; }
        public DateTime Timestamp { get; set; }
        public int Temperature { get; set; }
        public bool IsArchived { get; set; }

        public virtual City City { get; set; }
    }
}