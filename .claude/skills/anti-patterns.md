---
name: anti-patterns
description: >
  Banned patterns in DevSweep: no AutoMapper, no comments in production code,
  no deep inheritance hierarchies.
---

# Anti-Patterns (BANNED)

## No Reflection-Based Mapping
- BANNED: AutoMapper, Mapster, ExpressMapper
- USE: Explicit extension methods

```csharp
// WRONG
var dto = _mapper.Map<UserDto>(entity);

// CORRECT
public static class UserMappings
{
    public static UserDto ToDto(this UserEntity entity) => new(
        Id: entity.Id.ToString(),
        Name: entity.FullName,
        Email: entity.EmailAddress);
}

var dto = entity.ToDto();
```

## No Comments in Production Code
- NO comments — code must be self-documenting
- YES semantic names that make intent obvious

## No Deep Inheritance
- NO abstract base classes in application code
- NO inheritance hierarchies
- YES composition and interfaces
- YES records for modeling variants
