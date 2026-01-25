# .NET 8 C# Project Guidelines

## General Principles

- **Lean**: Keep the codebase minimal, avoid unnecessary dependencies, and refactor regularly.
- **Clean**: Ensure that the code follows a clear structure, utilizes proper naming conventions, and is easy to navigate.
- **Readable Code**: Write code as if the next developer is a psychopath who knows where you live; clarity is essential.

## Planning and Development Process

1. **Think Deeply**: Before starting any coding, take the time to thoroughly understand the requirements and design the solution.
2. **Plan First**: Document your approach, including architecture and design choices, in a planning document or a dedicated section here.
3. **Self-Review**: Review your own code and design choices critically. Check for adherence to coding standards, performance considerations, and overall design.
4. **Wait for Approval**: Submit your design or code for review before implementation. Ensure you have received feedback and approval from the team.

## Coding Standards

### Naming Conventions

- **Classes**: Use PascalCase (e.g., `CustomerController`, `OrderService`).
- **Methods**: Use PascalCase (e.g., `GetCustomerById`, `CalculateTotal`).
- **Variables**: Use camelCase (e.g., `orderTotal`, `userName`).
- **Constants**: Use ALL_CAPS with underscores (e.g., `MAX_USERS`, `PAGE_SIZE`).

### Method Structure

- Each method should have a single responsibility.
- Limit methods to a maximum of 20-30 lines.
- Use clear and meaningful names; avoid using vague prefixes (e.g., `DoSomething`).

### Comments and Documentation

- Use XML documentation comments for public methods and classes.
- Keep comments relevant and updated; they should explain why, not what.

### Exception Handling

- Use exceptions for exceptional cases, not for general flow control.
- Ensure that exceptions are logged appropriately.

## Performance Considerations

- **Avoid Unnecessary Allocations**: Use local variables and avoid boxing.
- **Use `foreach` Instead of `for`**: Prefer `foreach` when iterating collections, as it can lead to cleaner and more readable code.
- **Profiling**: Regularly profile the code to identify bottlenecks and optimize performance accordingly.

## Consistency Across the Codebase

- Follow the same architectural pattern throughout the application (e.g., MVC, Clean Architecture).
- Use dependency injection consistently.
- Ensure that developers use the same tooling and settings in their IDE (e.g., EditorConfig, formatting rules).

## Library Usage Policy

- **Don't Add Libraries Unless Permitted**: Introduce new libraries only after gaining approval from the team.
- **LINQ is Prohibited**: LINQ should not be used in the codebase. Use manual iteration and other standard methods for collection management and querying.

## Commit Message Guidelines

When committing changes, follow the structure below to maintain clarity:

### Format:
- **Type**: feat, fix, docs, style, refactor, test
- **Scope**: Optional
- **Description**: Brief, imperative

### Example Messages:
- **feat**: add support for user authentication
- **fix**: resolve issue with order calculation
- **docs**: update API documentation

## Conclusion

These guidelines help maintain a high-quality codebase that is lean, clean, and easy to work with, ensuring that all developers are aligned. Keep this document updated as the project evolves.
