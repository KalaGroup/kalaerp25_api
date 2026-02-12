using KalaGenset.ERP.Infra.UnitOfwork.Collections;
using KalaGenset.ERP.Infra.UnitOfWork;
using KalaGenset.ERP.Infra.UnitOfWork.Collections;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using System.Data.SqlTypes;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;

namespace KalaGenset.ERP.Infra
{
    public class Repository<TEntity> : IRepository<TEntity> where TEntity : class
    {
        protected readonly DbContext _dbcontext;
        protected readonly DbSet<TEntity> _dbset;


        public Repository(DbContext dbcontext)
        {
            _dbcontext = dbcontext ?? throw new ArgumentNullException(nameof(dbcontext));
            _dbset = _dbcontext.Set<TEntity>();
        }


        public virtual void ChangeTable(string table)
        {
            if (_dbcontext.Model.FindEntityType(typeof(TEntity)) is IConventionEntityType relational)
            {
                relational.SetTableName(table);
            }
        }

        public IQueryable<TEntity> GetAll()
        {
            return _dbset;
        }

        public IQueryable<TEntity> GetAll(
            Expression<Func<TEntity, bool>> predicate = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
            Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>> include = null, bool disableTracking = true, bool ignoreQueryFilters = false)
        {
            IQueryable<TEntity> query = _dbset;

            if (disableTracking)
            {
                query = query.AsNoTracking();
            }

            if (include != null)
            {
                query = include(query);
            }

            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            if (ignoreQueryFilters)
            {
                query = query.IgnoreQueryFilters();
            }

            if (orderBy != null)
            {
                return orderBy(query);
            }
            else
            {
                return query;
            }
        }

        public virtual IPagedList<TEntity> GetPagedList(Expression<Func<TEntity, bool>> predicate = null,
                                                   Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
                                                   Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>> include = null,
                                                   int pageIndex = 0,
                                                   int pageSize = 20,
                                                   bool disableTracking = true,
                                                   bool ignoreQueryFilters = false)
        {
            IQueryable<TEntity> query = _dbset;

            if (disableTracking)
            {
                query = query.AsNoTracking();
            }

            if (include != null)
            {
                query = include(query);
            }

            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            if (ignoreQueryFilters)
            {
                query = query.IgnoreQueryFilters();
            }

            if (orderBy != null)
            {
                return orderBy(query).ToPagedList(pageIndex, pageSize);
            }
            else
            {
                return query.ToPagedList(pageIndex, pageSize);
            }
        }

       
        public virtual Task<IPagedList<TEntity>> GetPagedListAsync(Expression<Func<TEntity, bool>> predicate = null,                                             
                                                   Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
                                                   Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>> include = null,
                                                   int pageIndex = 0,
                                                   int pageSize = 20,
                                                   bool disableTracking = true,
                                                   CancellationToken cancellationToken = default(CancellationToken),
                                                   bool ignoreQueryFilters = false)
            
        {
            IQueryable<TEntity> query = _dbset;

            if (disableTracking)
            {
                query = query.AsNoTracking();
            }

            if (include != null)
            {
                query = include(query);
            }

            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            if (ignoreQueryFilters)
            {
                query = query.IgnoreQueryFilters();
            }

            if (orderBy != null)
            {
                return orderBy(query).ToPagedListAsync(pageIndex, true, pageSize, 0, cancellationToken);
            }
            else
            {
                return query.ToPagedListAsync(pageIndex, true, pageSize, 0, cancellationToken);
            }
        }
        public virtual IPagedList<TResult>GetPagedList<TResult>(Expression<Func<TEntity,TResult>>selector,
            Expression<Func<TEntity, bool>> predicate = null,
                                                  Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
                                                  Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>> include = null,
                                                  int pageIndex = 0,
                                                  int pageSize = 20,
                                                  bool disableTracking = true,
                                                  bool ignoreQueryFilters = false)
            where TResult : class
        {
            IQueryable<TEntity> query = _dbset;

            if (disableTracking)
            {
                query = query.AsNoTracking();
            }

            if (include != null)
            {
                query = include(query);
            }

            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            if (ignoreQueryFilters)
            {
                query = query.IgnoreQueryFilters();
            }

            if (orderBy != null)
            {
                return orderBy(query).Select(selector).ToPagedList(pageIndex, pageSize);
            }
            else
            {
                return query.Select(selector).ToPagedList(pageIndex, pageSize);
            }
        }

        public virtual Task<IPagedList<TResult>> GetPagedListAsync<TResult>(Expression<Func<TEntity, TResult>> selector,
                                                   Expression<Func<TEntity, bool>> predicate = null,
                                                   Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
                                                   Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>> include = null,
                                                   int pageIndex = 0,
                                                   int pageSize = 20,
                                                   bool disableTracking = true,
                                                   CancellationToken cancellationToken = default(CancellationToken),
                                                   bool ignoreQueryFilters = false)
            where TResult : class
        {
            IQueryable<TEntity> query = _dbset;

            if (disableTracking)
            {
                query = query.AsNoTracking();
            }

            if (include != null)
            {
                query = include(query);
            }

            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            if (ignoreQueryFilters)
            {
                query = query.IgnoreQueryFilters();
            }

            if (orderBy != null)
            {
                return orderBy(query).Select(selector).ToPagedListAsync(pageIndex, true, pageSize, 0, cancellationToken);
            }
            else
            {
                return query.Select(selector).ToPagedListAsync(pageIndex, true, pageSize, 0, cancellationToken);
            }
        }

        public virtual TEntity GetFirstOrDefualt(Expression<Func<TEntity, bool>> predicate = null,

                              Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
                              Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>> include = null,
                              bool disableTracking = true,
                              bool ignoreQueryFilters = false)
        {
            IQueryable<TEntity> query = _dbset;

            if (disableTracking)
            {
                query = query.AsNoTracking();
            }

            if (include != null)
            {
                query = include(query);
            }

            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            if (ignoreQueryFilters)
            {
                query = query.IgnoreQueryFilters();
            }

            if (orderBy != null)
            {
                return orderBy(query).FirstOrDefault();
            }
            else
            {
                return query.FirstOrDefault();
            }
        }

        public virtual async Task<TEntity> GetFirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate = null,
                              Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
                              Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>> include = null,
                              bool disableTracking = true,
                              bool ignoreQueryFilters = false)
        {
            IQueryable<TEntity> query = _dbset;

            if (disableTracking)
            {
                query = query.AsNoTracking();
            }

            if (include != null)
            {
                query = include(query);
            }

            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            if (ignoreQueryFilters)
            {
                query = query.IgnoreQueryFilters();
            }

            if (orderBy != null)
            {
                return await orderBy(query).FirstOrDefaultAsync();
            }
            else
            {
                return await query.FirstOrDefaultAsync();
            }
        }

        public virtual TResult GetFirstOrDefault<TResult>(Expression<Func<TEntity, TResult>> selector,  
                              Expression<Func<TEntity, bool>> predicate = null,
                              Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
                              Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>> include = null,
                              bool disableTracking = true,
                              bool ignoreQueryFilters = false)
        {
            IQueryable<TEntity> query = _dbset;

            if (disableTracking)
            {
                query = query.AsNoTracking();
            }

            if (include != null)
            {
                query = include(query);
            }

            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            if (ignoreQueryFilters)
            {
                query = query.IgnoreQueryFilters();
            }

            if (orderBy != null)
            {
                return orderBy(query).Select(selector).FirstOrDefault();
            }
            else
            {
                return query.Select(selector).FirstOrDefault();
            }
        }

        public virtual async Task<TResult> GetFirstOrDefaultAsync<TResult>(Expression<Func<TEntity, TResult>> selector,
            Expression<Func<TEntity, bool>> predicate = null,
                              Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
                              Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>> include = null,
                              bool disableTracking = true,
                              bool ignoreQueryFilters = false)
        {
            IQueryable<TEntity> query = _dbset;

            if (disableTracking)
            {
                query = query.AsNoTracking();
            }

            if (include != null)
            {
                query = include(query);
            }

            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            if (ignoreQueryFilters)
            {
                query = query.IgnoreQueryFilters();
            }

            if (orderBy != null)
            {
                return await orderBy(query).Select(selector).FirstOrDefaultAsync();
            }
            else
            {
                return await query.Select(selector).FirstOrDefaultAsync();
            }
        }

        public virtual IQueryable<TEntity> FromSql(string sql, params object[] parameters) => _dbset.FromSqlRaw(sql, parameters);

        public virtual TEntity Find(params object[] keyValues) => _dbset.Find(keyValues);

        public virtual ValueTask<TEntity> FindAsync(params object[] keyValues) => _dbset.FindAsync(keyValues);

        public virtual ValueTask<TEntity> FindAsync(object[] keyValues, CancellationToken cancellationToken) => _dbset.FindAsync(keyValues, cancellationToken);

        public virtual int Count(Expression<Func<TEntity, bool>> predicate = null)
        {
            if (predicate == null)
            {
                return _dbset.Count();
            }
            else
            {
                return _dbset.Count(predicate);
            }
        }

        public virtual async Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate = null)
        {
            if (predicate == null)
            {
                return await _dbset.CountAsync();
            }
            else
            {
                return await _dbset.CountAsync(predicate);
            }
        }

        public virtual long LongCount(Expression<Func<TEntity, bool>> predicate = null)
        {
            if (predicate == null)
            {
                return _dbset.LongCount();
            }
            else
            {
                return _dbset.LongCount(predicate);
            }
        }

        public virtual async Task<long> LongCountAsync(Expression<Func<TEntity, bool>> predicate = null)
        {
            if (predicate == null)
            {
                return await _dbset.LongCountAsync();
            }
            else
            {
                return await _dbset.LongCountAsync(predicate);
            }
        }

        public virtual T Max<T>(Expression<Func<TEntity, bool>> predicate = null, Expression<Func<TEntity, T>> selector = null)
        {
            if (predicate == null)
                return _dbset.Max(selector);
            else
                return _dbset.Where(predicate).Max(selector);
        }

        public virtual async Task<T> MaxAsync<T>(Expression<Func<TEntity, bool>> predicate = null, Expression<Func<TEntity, T>> selector = null)
        {
            if (predicate == null)
                return await _dbset.MaxAsync(selector);
            else
                return await _dbset.Where(predicate).MaxAsync(selector);
        }

        public virtual T Min<T>(Expression<Func<TEntity, bool>> predicate = null, Expression<Func<TEntity, T>> selector = null)
        {
            if (predicate == null)
                return _dbset.Min(selector);
            else
                return _dbset.Where(predicate).Min(selector);
        }

        public virtual async Task<T> MinAsync<T>(Expression<Func<TEntity, bool>> predicate = null, Expression<Func<TEntity, T>> selector = null)
        {
            if (predicate == null)
                return await _dbset.MinAsync(selector);
            else
                return await _dbset.Where(predicate).MinAsync(selector);
        }

        public virtual Decimal Average(Expression<Func<TEntity, bool>> predicate = null, Expression<Func<TEntity, decimal>> selector = null)
        {
            if (predicate == null)
                return _dbset.Average(selector);
            else
                return _dbset.Where(predicate).Average(selector);
        }

        public virtual async Task<decimal> AverageAsync(Expression<Func<TEntity, bool>> predicate = null, Expression<Func<TEntity, decimal>> selector = null)
        {
            if (predicate == null)
                return await _dbset.AverageAsync(selector);
            else
                return await _dbset.Where(predicate).AverageAsync(selector);
        }

        public virtual Decimal Sum(Expression<Func<TEntity, bool>> predicate = null, Expression<Func<TEntity, decimal>> selector = null)
        {
            if (predicate == null)
                return _dbset.Sum(selector);
            else
                return _dbset.Where(predicate).Sum(selector);
        }

        //public virtual async Task<decimal> SumAsync(Expression<Func<TEntity, bool>> predicate = null, Expression<Func<TEntity, decimal>> selector = null)
        //{
        //    if (predicate == null)
        //        return await _dbset.SumAsync(selector);
        //    else
        //        return await _dbset.Where(predicate).SumAsync(selector);
        //}

        public bool Exists(Expression<Func<TEntity, bool>> selector = null)
        {
            if (selector == null)
            {
                return _dbset.Any();
            }
            else
            {
                return _dbset.Any(selector);
            }
        }

        public async Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> selector = null)
        {
            if (selector == null)
            {
                return await _dbset.AnyAsync();
            }
            else
            {
                return await _dbset.AnyAsync(selector);
            }
        }

        public virtual TEntity Insert(TEntity entity)
        {
            return _dbset.Add(entity).Entity;
        }

        public virtual void Insert(params TEntity[] entities) => _dbset.AddRange(entities);
        public virtual void Insert(IEnumerable<TEntity> entities) => _dbset.AddRange(entities);

        public virtual ValueTask<EntityEntry<TEntity>> InsertAsync(TEntity entity, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _dbset.AddAsync(entity, cancellationToken);
        }

        public virtual Task InsertAsync(params TEntity[] entities) => _dbset.AddRangeAsync(entities);

        public virtual Task InsertAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default(CancellationToken)) => _dbset.AddRangeAsync(entities, cancellationToken);

        public virtual void Update(TEntity entity)
        {
            _dbset.Update(entity);
        }

        public virtual void UpdateAsync(TEntity entity)
        {
            _dbset.Update(entity);
        }

        public virtual void Update(params TEntity[] entities) => _dbset.UpdateRange(entities);

        public virtual void Update(IEnumerable<TEntity> entities) => _dbset.UpdateRange(entities);

        public virtual void Delete(TEntity entity) => _dbset.Remove(entity);

        public virtual void Delete(object id)
        {
            var typeInfo = typeof(TEntity).GetTypeInfo();
            var key = _dbcontext.Model.FindEntityType(typeInfo).FindPrimaryKey().Properties.FirstOrDefault();
            var property = typeInfo.GetProperty(key?.Name);
            if (property != null)
            {
                var entity = Activator.CreateInstance<TEntity>();
                property.SetValue(entity, id);
                _dbcontext.Entry(entity).State = EntityState.Deleted;
            }
            else
            { 
               var entity = _dbset.Find(id);
                if (entity != null)
                {
                    Delete(entity);
                }
            }
        }

        public virtual void Delete(params TEntity[] entities) => _dbset.RemoveRange(entities);

        public virtual void Delete(IEnumerable<TEntity> entities) => _dbset.RemoveRange(entities);

        public async Task<IList<TEntity>> GetAllAsync()
        {
            return await _dbset.ToListAsync();
        }


        public async Task<IList<TEntity>> GetAllAsync(Expression<Func<TEntity, bool>> predicate = null,
                                                   Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
                                                   Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>> include = null,
                                                   bool disableTracking = true,
                                                   bool ignoreQueryFilters = false)
        {
            IQueryable<TEntity> query = _dbset;

            if (disableTracking)
            {
                query = query.AsNoTracking();
            }

            if (include != null)
            {
                query = include(query);
            }

            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            if (ignoreQueryFilters)
            {
                query = query.IgnoreQueryFilters();
            }

            if (orderBy != null)
            {
                return await orderBy(query).ToListAsync();
            }
            else
            {
                return await query.ToListAsync();
            }
        }

        public void ChangeEntityState(TEntity entity, EntityState state)
        { 
          _dbcontext.Entry(entity).State = state;
        }
    }
}