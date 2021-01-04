using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using FakeXiecheng.API.ValidationAttributes;

namespace FakeXiecheng.API.Dtos
{
    public class TouristRouteForUpdateDto: TouristRouteForManipulationDto
    {
        [Required(ErrorMessage = "update must")]
        [MaxLength(1500)]
        public override string Description { get; set; }
    }
}
