select p.NickName, p.LastName, a.ScheduledToAttend, a.RequestedToAttend, ao.OccurrenceDate, g.Name [Group.Name], l.Name [Location.Name], s.Name [Schedule.Name]
from Attendance a
join AttendanceOccurrence ao on a.OccurrenceId = ao.Id
join [Group] g on ao.GroupId = g.Id
join PersonAlias pa on a.PersonAliasId = pa.Id
join Person p on pa.PersonId = p.Id
join [Location] l on ao.LocationId = l.Id
join Schedule s on ao.ScheduleId= s.Id
where a.ScheduledToAttend is not null