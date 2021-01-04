using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AutoMapper;
using FakeXiecheng.API.Dtos;
using FakeXiecheng.API.Helpers;
using FakeXiecheng.API.Models;
using FakeXiecheng.API.ResourceParameters;
using FakeXiecheng.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace FakeXiecheng.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TouristRoutesController: ControllerBase
    {
        private readonly  ITouristRouteRepository _touristRouteRepository;
        private readonly IMapper _mapper;
        public TouristRoutesController(ITouristRouteRepository touristRouteRepository,
            IMapper mapper)
        {
            _touristRouteRepository = touristRouteRepository;
            _mapper = mapper;
        }
        //api/touristRoutes?keyword=input content
        [HttpGet]
        [HttpHead]
        public IActionResult GetTouristRoutes(
            [FromQuery] TouristRouteResourceParamaters paramaters
            //[FromQuery] string keyword,
            //string rating //lessThan, largerThan, equalTo
            )//fromquery or frombody
        {
            var touristRoutesFromRepo = _touristRouteRepository
                .GetTouristRoutes(paramaters.Keyword, paramaters.RatingOperator, paramaters.RatingValue);
            if (touristRoutesFromRepo == null || !touristRoutesFromRepo.Any())
            {
                return NotFound("no tourist route");
            }

            var touristRouteDto = _mapper.Map<IEnumerable<TouristRouteDto>>(touristRoutesFromRepo);
            return Ok(touristRouteDto);
        }

        [HttpGet("{touristRouteId:Guid}", Name = "GetTouristRouteById")]
        [HttpHead]
        public IActionResult GetTouristRouteById(Guid touristRouteId)
        {
            var touristRouteFromRepo = _touristRouteRepository.GetTouristRoute(touristRouteId);
            if (touristRouteFromRepo == null)
            {
                return NotFound($"tourist route {touristRouteId} cannot find");
            }

            var touristRouteDto = _mapper.Map<TouristRouteDto>(touristRouteFromRepo);

            return Ok(touristRouteDto);
        }

        [HttpPost]
        public IActionResult CreateTouristRoute([FromBody] TouristRouteForCreationDto touristRouteForCreationDto)
        {
            var touristRouteModel = _mapper.Map<TouristRoute>(touristRouteForCreationDto);
            _touristRouteRepository.AddTouristRoute(touristRouteModel);
            _touristRouteRepository.Save();

            var touristRouteToReturn = _mapper.Map<TouristRouteDto>(touristRouteModel);
            return CreatedAtRoute(
                "GetTouristRouteById",
                new {touristRouteId = touristRouteToReturn.Id},
                touristRouteToReturn);
        }

        [HttpPut("{touristRouteId}")]
        public IActionResult UpdateTouristRoute(
            [FromRoute] Guid touristRouteId,
            [FromBody] TouristRouteForUpdateDto touristRouteForUpdateDto
            )
        {
            if (! _touristRouteRepository.TouristRouteExists(touristRouteId))
            {
                return NotFound("Cannot find the tourist route");
            }

            var touristRouteFromRepo = _touristRouteRepository.GetTouristRoute(touristRouteId);
            //1. 映射Dto
            //2. 更新dto
            //3. 映射model
            _mapper.Map(touristRouteForUpdateDto, touristRouteFromRepo);
            _touristRouteRepository.Save();

            return NoContent();
        }

        [HttpPatch("{touristRouteId}")]
        public IActionResult PartiallyUpdateTouristRoute(
            [FromRoute]Guid touristRouteId,
            [FromBody] JsonPatchDocument<TouristRouteForUpdateDto> patchDocument)
        {
            if (!_touristRouteRepository.TouristRouteExists(touristRouteId))
            {
                return NotFound("tourist route cannot find");
            }

            var touristRouteFromRepo = _touristRouteRepository.GetTouristRoute(touristRouteId);
            var touristRouteToPatch = _mapper.Map<TouristRouteForUpdateDto>(touristRouteFromRepo);
            patchDocument.ApplyTo(touristRouteToPatch, ModelState);
            if (TryValidateModel(touristRouteToPatch))
            {
                return ValidationProblem(ModelState);
            }
            _mapper.Map(touristRouteToPatch, touristRouteFromRepo);
            _touristRouteRepository.Save();

            return NoContent();
        }
        [HttpDelete("{touristRouteId}")]
        public IActionResult DeleteTouristRoute([FromRoute] Guid touristRouteId)
        {
            if (! _touristRouteRepository.TouristRouteExists(touristRouteId))
            {
                return NotFound("the tourist route does not exist");
            }

            var touristRoute = _touristRouteRepository.GetTouristRoute(touristRouteId);
            _touristRouteRepository.DeleteTouristRoute(touristRoute);
            _touristRouteRepository.Save();

            return NoContent();
        }

        [HttpDelete("({touristIDs})")]
        public IActionResult DeleteByIDs(
            [ModelBinder(BinderType = typeof(ArrayModelBinder))]
            [FromRoute] IEnumerable<Guid> touristIDs)
        {
            if (touristIDs == null)
            {
                return BadRequest();
            }

            var touristRoutesFromRepo = _touristRouteRepository.GetTouristRoutesByIDList(touristIDs);
            _touristRouteRepository.DeleteTouristRoutes(touristRoutesFromRepo);
            _touristRouteRepository.Save();

            return NoContent();
        }
    }
}
 