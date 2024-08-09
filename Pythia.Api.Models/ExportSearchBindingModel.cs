using System;
using System.ComponentModel.DataAnnotations;

namespace Pythia.Api.Models;

public class ExportSearchBindingModel : SearchBindingModel
{
    /// <summary>
    /// Gets or sets the last page to return. If not specified, all the pages
    /// will be returned.
    /// </summary>
    [Range(0, int.MaxValue)]
    public int? LastPage { get; set; }
}
