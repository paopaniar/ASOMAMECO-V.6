using Asomameco.Application.DTOs;
using Asomameco.Application.Services.Interfaces;
using Asomameco.Infraestructure.Repository.Interfaces;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asomameco.Application.Services.Implementations
{
    public class ServiceLugar: IServiceLugar
    {

        private readonly IRepositoryLugar _repository;
        private readonly IMapper _mapper;

        public ServiceLugar(IRepositoryLugar repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }
        public async Task<LugarDTO> FindByIdAsync(int id)
        {
            var @object = await _repository.FindByIdAsync(id);
            var objectMapped = _mapper.Map<LugarDTO>(@object);
            return objectMapped;
        }
        public async Task<ICollection<LugarDTO>> ListAsync()
        {
            //Obtener datos del repositorio
            var list = await _repository.ListAsync();
            var collection = _mapper.Map<ICollection<LugarDTO>>(list);
            // Return lista
            return collection;
        }
    }
}
