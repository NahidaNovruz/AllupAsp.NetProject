﻿using Allup1.Extensions;
using AllupPractice.DAL;
using AllupPractice.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AllupPractice.Areas.AdminAllup.Controllers
{
    [Area("AdminAllup")] 
    public class CategoryController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        public CategoryController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public IActionResult Index()
        {
            return View(_context.Categories.Where(c => c.IsDeleted == false).OrderByDescending(c => c.Id).ToList()); 
        }

        public IActionResult Create()
        {
            GetMainCategory(); 
            return View();
        }

        [HttpPost] 
        [ValidateAntiForgeryToken] 
        public async Task<IActionResult> Create(Category category, int? MainCtgId) 
        {
            GetMainCategory();
            if (!ModelState.IsValid) return View();
            if (category.IsMain)
            {
                bool isExist = _context.Categories.Where(c => c.IsDeleted == false && c.IsMain == true)
                    .Any(c => c.Name.Trim().ToLower() == category.Name.Trim().ToLower());

                if (isExist)
                {
                    ModelState.AddModelError("Name", "This category already exist");
                    return View();
                }
                if (category.Photo == null)
                {
                    ModelState.AddModelError("", "Please add image,if create main category");
                    return View();
                }
                if (!category.Photo.IsImage())
                {
                    ModelState.AddModelError("", "Image size should be less than 200kb");
                    return View();
                }

                if (!category.Photo.MaxSize(200))
                {
                    ModelState.AddModelError("", "Image size should be less than 200kb");
                    return View();
                }
                string folder = Path.Combine("assets", "images"); 
                string filename = await category.Photo.SaveImageAsync(_env.WebRootPath, folder);
                category.Image = filename;
                category.IsDeleted = false;
            }
            else
            {
                if (MainCtgId == null)
                {
                    ModelState.AddModelError("", "Select main category");
                    return View();
                }
                Category mainCategory = _context.Categories.Include(c => c.Children)
                    .FirstOrDefault(c => c.Id == MainCtgId && c.IsDeleted == false);
                if (mainCategory == null)
                {
                    return NotFound();
                }
                bool isExist = mainCategory.Children
                    .Any(cc => cc.Name.Trim().ToLower() == category.Name.Trim().ToLower());

                if (isExist)
                {
                    ModelState.AddModelError("Name", $"This category already exist in {mainCategory.Name}");
                    return View();
                }
                category.Parent = mainCategory;
            }

            await _context.Categories.AddAsync(category);
            await _context.SaveChangesAsync(); 
            return RedirectToAction(nameof(Index));
        }

        private void GetMainCategory()
        {
            ViewBag.MainCtg = _context.Categories.Where(c => c.IsDeleted == false && c.IsMain == true).ToList();
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            Category category = _context.Categories.Include(c => c.Children).Include(c => c.Parent)
                .FirstOrDefault(c => c.IsDeleted == false && c.Id == id);
            if (category == null) return NotFound();
            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Delete")]
        public async Task<IActionResult> DeletePost(int? id)
        {
            if (id == null) return NotFound();
            Category category = _context.Categories.Include(c => c.Children).Include(c => c.Parent)
                .FirstOrDefault(c => c.IsDeleted == false && c.Id == id);
            if (category == null) return NotFound();

            category.IsDeleted = true;
            if (category.Children.Count() > 0)
            {
                List<Category> children = category.Children.Where(c => c.IsDeleted == false).ToList();
                foreach (Category ctgchild in children)
                {
                    ctgchild.IsDeleted = true;
                }
            }
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }


        //public IActionResult Update(int? id)
        //{
        //    if (id == null) return NotFound();
        //    Category dbCategory = _context.Categories.FirstOrDefault(c => c.Id == id); 
        //    if (dbCategory == null) return NotFound();
        //    return View(dbCategory);
        //}

        //public IActionResult Detail(int? id)
        //{
        //    if (id == null) return NotFound();
        //    Category category = _context.Categories.Where(c => c.IsDeleted == false)
        //        .Include(c => c.Children).Include(c => c.Parent)
        //            .FirstOrDefault(c => c.Id == id);
        //    if (category == null) return NotFound();
        //    return View(category);
        //}

        public async Task<IActionResult> Update(int? id)
        {
            GetMainCategory();
            if (id == null) return NotFound();
            Category category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();

            await _context.SaveChangesAsync();
            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(Category category, int? id)
        {
            GetMainCategory();
            if (id == null) return NotFound();
            Category updateCtr = await _context.Categories.FindAsync(id);
            if (category.Photo == null)
            {
                ModelState.AddModelError("", "Please add image,if create main category");
                return View();
            }
            Category isExist = _context.Categories.Where(c => c.IsDeleted == false).FirstOrDefault(c => c.Name.ToLower() == category.Name.ToLower());
            if (category == null) return NotFound();

            updateCtr.Name = category.Name;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

    }
}
