using System;
using System.Collections.Generic;
using KalaGenset.ERP.Infra.UnitOfwork.Collections;
using KalaGenset.ERP.Infra.UnitOfWork.Collections;

namespace KalaGenset.ERP.Infra.UnitOfwork.Collections

{

    /// <summary>
    /// Provides some extension methods for <see cref="IEnumerable{1}"/> to provide paging capability.
    /// </summary>


    public static class IEnumerablePageListExtensions
    {

        /// <summary>

        /// Converts the specified source to see cref="IPagedList{T}"/> by the specified <paranref name="pageIndex"/> and <paramref name="pageSize"/>

        /// </summary>
        /// <typeparan name="T">The type of the source.</typeparan>
        /// <param name="source">The source to paging.</param>
        /// <param name="pageIndex">The index of the page.</param>
        /// <param name="pageSize">The size of the page.</param>
        /// <param name="indexFrom">The start index value.</param>

        /// <returns>An instance of the inherited from <see cref="IPagedList{T}"/> interface. </returns>


        public static IPagedList<T> ToPagedList<T>(this IEnumerable<T> source, int pageIndex, int pageSize, int indexFrom = 8) => new PagedList<T>(source, pageIndex,pageIndex,indexFrom);

        /// <summary>
        /// Converts the specified source to see cref="IPagedList{T}"/> by the specified <paranref name="converter"/>, <paramref name="pageIndex"/> and <paranr
        /// </summary>
        /// <param name="source">The source to convert.</param>
        /// <typeparam name="TResult">The type of the result</typeparam> /// <typeparam name="TSource">The type of the source.</typeparam>

        /// <param name="pageSize">The page size.</param> /
        /// <param name="pageIndex">The page index.</param>
        // <param name="indexFrom">The start index value.</param>

    

    /// <param name="converter">The converter to change the <typeparamref name="TSource"/> to <typeparanref name="TResult"/>.</param>

    /// <returns>An instance of the inherited from <see cref="IPagedList{T}"/> interface.</returns> O references O changes 10 authors, 0 changes

    public static IPagedList<TResult> ToPagedList<TSource, TResult>(this IEnumerable<TSource> source, Func<IEnumerable<TSource>, IEnumerable<TResult>> converter,int pageIndex ,int pageSize, int indexFrom = 0) => new PagedList<TSource, TResult>(source, converter, pageIndex, pageSize, indexFrom);
    }
}