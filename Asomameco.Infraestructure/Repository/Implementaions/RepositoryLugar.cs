using Asomameco.Infraestructure.Data;
using Asomameco.Infraestructure.Models;
using Asomameco.Infraestructure.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asomameco.Infraestructure.Repository.Implementaions
{
    public class RepositoryLugar: IRepositoryLugar
    {

        private readonly AsomamecoContext _context;
        public RepositoryLugar(AsomamecoContext context)
        {
            _context = context;
        }

        public async Task<Lugar> FindByIdAsync(int id)
        {

            var @object = await _context.Set<Lugar>().FindAsync(id);

            return @object!;
        }

        public async Task<ICollection<Lugar>> ListAsync()
        {
            var collection = await _context.Set<Lugar>().ToListAsync();
            return collection;
        }
    }
}
