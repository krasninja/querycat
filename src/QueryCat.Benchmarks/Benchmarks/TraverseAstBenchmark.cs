using BenchmarkDotNet.Attributes;
using QueryCat.Backend.Ast;

namespace QueryCat.Benchmarks.Benchmarks;

[MemoryDiagnoser]
public class TraverseAstBenchmark
{
    private readonly IAstNode _node;
    private readonly AstTraversal _astTraversal = new(new CallbackDelegateVisitor());

    public TraverseAstBenchmark()
    {
        _node = new Backend.Parser.AstBuilder()
            .BuildProgramFromString("""
                                    select 1, 2, 3, 4, 5, 6, 7, 8, 9
                                    union
                                    select 1, 2, 3, 4, 5, 6, 7, 8, 9
                                    union
                                    select 1, 2, 3, 4, 5, 6, 7, 8, 9
                                    union
                                    select 1, 2, 3, 4, 5, 6, 7, 8, 9
                                    union
                                    select 1, 2, 3, 4, 5, 6, 7, 8, 9
                                    union
                                    select 1, 2, 3, 4, 5, 6, 7, 8, 9
                                    union
                                    select 1, 2, 3, 4, 5, 6, 7, 8, 9
                                    union
                                    select 1, 2, 3, 4, 5, 6, 7, 8, 9
                                    union
                                    select 1, 2, 3, 4, 5, 6, 7, 8, 9;

                                    select distinct on
                                        (u.id, dep.DepartmentId)
                                        u.id as 'userId',
                                        u.first_name as 'firstName',
                                        u.last_name as 'lastName',
                                        uinfo.education as 'Education',
                                        uinfo.fullName as 'fullName',
                                        u.birthday as 'birthdate',
                                        dep.DepartmentId as 'departmentId',
                                        branch.BranchId as 'branchId',
                                        u.primary_email as 'primaryEmail',
                                        (select us.id from saritasa_crm_user_supervisors() as us where us.supervisee_id = u.id) as supervisorId,
                                        uinfo.supervisorId as supervisorId,
                                        uinfo.techManagerId as techManagerId,
                                        case u.id
                                          when 243 then 'Regional Manager'
                                          when 261 then 'Director'
                                          when 652 then 'President'
                                          else (select "Name" from 'P.csv' as pos where pos.PositionId::string = uinfo.PositionId::string)
                                        end as "Position",
                                        uinfo.PositionId::string as PositionId,
                                        u.photo_url as 'avatar'
                                    into 'All.csv'
                                    from saritasa_crm_employees() as u
                                    left join (
                                      select * from 'U1.csv' as uinfo1
                                        where not exists(select uinfoa.userId from 'U2.csv' as uinfoa where uinfo1.userId = uinfoa.userId) union select * from 'UsersInfoAfter.csv'
                                    ) as uinfo on uinfo.userId = u.id
                                    left join 'B.csv' as branch on branch.BranchId = uinfo.branchId
                                      or (branch.BranchId in (1, 5, 8) and uinfo.userId in (105, 108, 243))
                                      or (branch.BranchId in (6) and uinfo.userId in (105, 108))
                                      or (branch.BranchId = 2 and uinfo.userId in (105, 108, 243, 261))
                                      or (branch.BranchId = 10 and uinfo.userId in (105, 108, 1640))
                                    left join 'D.csv' as dep on dep.DepartmentId = uinfo.departmentId
                                      or (dep.DepartmentId = 20 and uinfo.userId in (106, 108))
                                      or (dep.DepartmentId = 18 and uinfo.userId in (103, 1043))
                                      or (dep.DepartmentId = 17 and uinfo.userId in (103))
                                      or (dep.DepartmentId = 16 and uinfo.userId in (103, 1043))
                                      or (dep.DepartmentId = 15 and uinfo.userId in (103, 1043))
                                      or (dep.DepartmentId = 14 and uinfo.userId in (105, 652, 1640))
                                      or (dep.DepartmentId = 21 and uinfo.userId in (103, 1043))
                                      or (dep.DepartmentId = 13 and uinfo.userId in (103, 1043))
                                      or (dep.DepartmentId = 12 and uinfo.userId in (108, 105, 243, 246))
                                      or (dep.DepartmentId = 11 and uinfo.userId in (108, 105, 243))
                                      or (dep.DepartmentId = 10 and uinfo.userId in (103, 1043))
                                      or (dep.DepartmentId = 9 and uinfo.userId in (103, 1043))
                                      or (dep.DepartmentId = 8 and uinfo.userId in (103, 1043))
                                      or (dep.DepartmentId = 7 and uinfo.userId in (103, 1043))
                                      or (dep.DepartmentId = 6 and uinfo.userId in (103, 1043))
                                      or (dep.DepartmentId = 5 and uinfo.userId in (105, 108))
                                      or (dep.DepartmentId = 4 and uinfo.userId in (105, 108))
                                      or (dep.DepartmentId = 3 and uinfo.userId in (108, 612, 641, 1786))
                                    where (u.status = 'Active' or u.status = 'Vacation')
                                      and (u.on_site = true or uinfo.branchId in (3, 8, 10) or u.id in (1238, 1723, 1792, 1905))
                                      and u.id not in (1755, 1867)
                                      and u.id in (105, 243, 261, 684, 1374, 1482, 1640, 1723, 1856, 1897, 1905)
                                    order by u.id;
                                    """);
    }

    [Benchmark]
    public async Task TraverseComplexSqlQuery()
    {
        await _astTraversal.PreOrderAsync(_node, CancellationToken.None);
    }
}
