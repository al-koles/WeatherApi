﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace WeatherApi.Models
{
    public partial class City
    {
        public City()
        {
            Measurements = new HashSet<Measurement>();
        }

        public int CityId { get; set; }
        public string CityName { get; set; }

        public virtual ICollection<Measurement> Measurements { get; set; }
    }
}