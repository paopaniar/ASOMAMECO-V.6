using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asomameco.Application.DTOs
{
    public record class LugarDTO
    {
        public int Id { get; set; }

        public string NombreLugar { get; set; } = null!;

        public string DireccionExacta { get; set; } = null!;

        public bool Estado { get; set; }


    }
}
