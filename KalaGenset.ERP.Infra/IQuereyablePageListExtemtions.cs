using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KalaGenset.ERP.Infra.UnitOfWork.Collections;
using Microsoft.EntityFrameworkCore;
namespace KalaGenset.ERP.Infra.UnitOfWork.Collections
{
    public static class IQuereyablePageListExtemtions
    {

        /// Converts the specified source to see cref=*IPagedList[1]"/> by the specified paranrof name="pageIndex"/> and <paranrof name="pageSize"/>
        /// <summary>
        /// </summary /// <typeparan name="T">The type of the source.</typeparan
        /// <param name="source">The source to paging.</param>
        /// <param name="pageIndex">The index of the page.</param> /// <param name="pageSize">The size of the page.</param>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        /// <param name="indexFrom">The start index value.</paran
        public static async Task<IPagedList<T>> ToPagedListAsync<T>(this IQueryable<T> source, int pageIndex, bool isPagination, int pagesize, int indexFrom = 0,
            CancellationToken cancellationToken=default(CancellationToken))
        {
            var count = 0;

            if (indexFrom > pageIndex)
            {

                throw new ArgumentException($"indexFrom: (indexFron) pageIndex: (pageIndex), must indexFron< pageIndex");
            }
            if (isPagination)
            {

                count = await source.CountAsync(cancellationToken).ConfigureAwait(false);
            }

            var items = await source.Skip((pageIndex - indexFrom) * pagesize).Take(pagesize).ToListAsync(cancellationToken).ConfigureAwait(false);

            var pagedList = new PagedList<T>()
            {
                PageIndex = pageIndex,
                PageSize = pagesize,
                IndexFrom = indexFrom,
                TotalCount = count,
                Items = items,
                TotalPages = (int)Math.Ceiling(count / (double)pagesize)
            };

            return pagedList;

        }
    } 
}