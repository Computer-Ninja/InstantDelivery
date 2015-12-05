﻿using AutoMapper;
using AutoMapper.QueryableExtensions;
using InstantDelivery.Common.Enums;
using InstantDelivery.Domain;
using InstantDelivery.Domain.Entities;
using InstantDelivery.Model;
using InstantDelivery.Service.Paging;
using System.Data.Entity;
using System.Linq;
using System.Web.Http;

namespace InstantDelivery.Service.Controllers
{
    [Authorize]
    [RoutePrefix("api/Employees")]
    public class EmployeesController : ApiController
    {
        private readonly InstantDeliveryContext context;

        public EmployeesController(InstantDeliveryContext context)
        {
            this.context = context;
        }

        /// <summary>
        /// Zwraca dane pracownika o podanym identyfikatorze
        /// </summary>
        /// <param name="id">Id pracownika</param>
        /// <returns>Dane pracownika</returns>
        public IHttpActionResult Get(int id)
        {
            var employee = context.Employees.Find(id);
            if (employee == null)
            {
                return NotFound();
            }
            return Ok(Mapper.Map<EmployeeDto>(employee));
        }

        [Route("Page"), HttpGet]
        public IHttpActionResult GetPage([FromUri] PageQuery query, string firstName = "",
            string lastName = "", string email = "")
        {
            var employees = context.Employees.AsQueryable();
            employees = ApplyFilters(employees, firstName, lastName, email);
            var dtos = employees.ProjectTo<EmployeeDto>();
            return Ok(PagingHelper.GetPagedResult(dtos, query));
        }

        [Route("Packages/Page"), HttpGet]
        public IHttpActionResult PackagesPage([FromUri] PageQuery query, string firstName = "",
            string lastName = "", string email = "")
        {
            var employees = context.Employees.AsQueryable();
            employees = ApplyFilters(employees, firstName, lastName, email);
            var dtos = employees.Include(e => e.Packages)
                                .ProjectTo<EmployeePackagesDto>();
            return Ok(PagingHelper.GetPagedResult(dtos, query));
        }

        [Route("Vehicles/Page"), HttpGet]
        public IHttpActionResult VehiclesPage([FromUri] PageQuery query, string firstName = "",
                string lastName = "", string email = "")
        {
            var employees = context.Employees.AsQueryable();
            employees = ApplyFilters(employees, firstName, lastName, email);
            var dtos = employees.Include(e => e.Vehicle)
                                .Include(e => e.Vehicle.VehicleModel)
                                .ProjectTo<EmployeeVehicleDto>();
            return Ok(PagingHelper.GetPagedResult(dtos, query));
        }

        /// <summary>
        /// Dodaje pracownika do bazy danych
        /// </summary>
        /// <param name="newEmployee">Nowy pracownik</param>
        public IHttpActionResult Post(EmployeeAddDto newEmployee)
        {
            context.Employees.Add(Mapper.Map<Employee>(newEmployee));
            context.SaveChanges();
            //TODO: return 201
            return Ok(newEmployee.Id);
        }

        /// <summary>
        /// Zmienia dane pracownika
        /// </summary>
        /// <param name="employee"></param>
        /// <returns></returns>
        public IHttpActionResult Put(EmployeeDto employee)
        {
            var oldEmployee = context.Employees.Find(employee.Id);
            if (oldEmployee == null)
            {
                return NotFound();
            }
            Mapper.Map(employee, oldEmployee);
            context.SaveChanges();
            return Ok();
        }

        /// <summary>
        /// Usuwa pracownika z bazy danych
        /// </summary>
        /// <param name="id">Id pracownika</param>
        public IHttpActionResult Delete(int id)
        {
            var employee = context.Employees.Find(id);
            foreach (var package in employee.Packages
                        .Where(p => p.Status == PackageStatus.InDelivery))
            {
                package.Status = PackageStatus.New;
            }
            context.Employees.Remove(employee);
            context.SaveChanges();
            return Ok();
        }

        /// <summary>
        /// Zmienia pojazd przypisany do pracownika
        /// </summary>
        /// <param name="employeeId">Id pracownika</param>
        /// <param name="vehicleId">Id pojazdu</param>
        [Route("ChangeVehicle"), HttpPost]
        public IHttpActionResult ChangeVehicle(int employeeId, int? vehicleId = null)
        {
            var owner = context.Employees.Include(e => e.Vehicle)
                                         .FirstOrDefault(e => e.Id == employeeId);
            if (owner == null)
            {
                return NotFound();
            }
            else
            {
                var vehicle = context.Vehicles.Find(vehicleId);
                owner.Vehicle = vehicle;
                context.SaveChanges();
                return Ok();
            }
        }

        private IQueryable<Employee> ApplyFilters(IQueryable<Employee> source, string firstName,
            string lastName, string email)
        {
            var result = source;
            if (!string.IsNullOrEmpty(firstName))
            {
                result = result.Where(e => e.FirstName.StartsWith(firstName));
            }
            if (!string.IsNullOrEmpty(lastName))
            {
                result = result.Where(e => e.LastName.StartsWith(lastName));
            }
            if (!string.IsNullOrEmpty(email))
            {
                result = result.Where(e => e.Email.StartsWith(email));
            }
            return result;
        }
    }
}
