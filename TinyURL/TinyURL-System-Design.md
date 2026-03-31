# TinyURL System Design

## Table of Contents

- [Introduction](#introduction)
- [Requirements](#requirements)
- [Data Model](#data-model)
- [API Design](#api-design)
- [Key Questions](#key-questions)
- [Basic Implementation](#basic-implementation)
- [Advanced Implementation](#advanced-implementation)
- [Create URL Flow](#Create-URL-Flow)
- [Redirect URL Flow](#Redirect-URL-Flow)
- [Additional discussion points](#Additional-discussion-points)

## Introduction

Tiny URL (URL shortener) is one of the most popular system design questions out there. On the surface it can appear as a very simple problem to solve, but it is possible to go deep on some of the scalability issues which many interviewers will expect.

This example will cover two solutions, a basic implementation and an advanced implementation. The basic implementation, has scalability issues but is worth noting, as discussing those flaws and a potential solution in the advanced implementation, will show a depth of understanding most candidates will not have.

## Requirements

### Functional Requirements
- Given a long URL create a short URL
- Given a short URL redirect to a long URL
### Non Functional Requirements
- Very low latency
- Very high availability
### Not covered
- Updating of URLs
- Deleting of URLs

## Data Model
This data model contains one table urls which has the following fields:

- id: uuid
- longUrl: The URL provided by the user.
- shortUrl: The generated short URL which will be used to redirect users.
- createdAt: When the short URL was created.

## API Design

Given the simplicity of the of data, a classic RESTful API could be used. RESTful APIs are simple, widely used, stateless, and support caching which make it a perfect candidate for our system.

Our REST API will comprise of two main endpoints:

1. POST: /create-url
    - Params: longURL
    - Status code: 201 created

2. GET: /{short-url}
    - Status code: 301 permanent redirect

From the data model we can see that the data is not deeply nested or interrelated which would negate many of the benefits of using a GraphQL API.

## Key Questions

1. How long should the URL be?
    - Need to know the scale of the application to answer this question.
    - Example: 1,000 URLs generated per second.
        + 1,000 * 60 * 60 * 24 * 365 = 31.5 billion URLs created each year
        + 10 to 1 read to write requests means ≈ 300 billion reads per year

2. What characters can we use?
    - Alphanumeric:
    + a - z: 26 characters
    + A - Z: 26 characters
    + 0 - 9: 10 characters
    + Total: 62 characters

    - Unique short URLs (varying characters):
    + 1 character: 621 → 62 URLs
    + 2 characters: 622 → 3,844 URLs
    + ...
    + 6 characters: 626 → ~56 billion URLs
    + 7 characters: 627 → ~3.5 trillion URLs

## Basic Implementation

![basic-implementation-short-url](./images/basic-implmentation-short-url.avif)

- In the basic implementation we could have a client make a request which goes to a load balancer which then distributes the request to a web server. This web server will receive a base 10 number from the count cache which it will then convert to base 62 and be used as the short URL. This short URL can then be stored in a database and returned to the user.
- While this does technically work, it will not be able to handle the scale discussed previously. It also introduces single points of failure in the web server, count cache, and database and so a better solution is needed.

![basic-implementation-2-basic-url](./images/basic-implementation-2-basic-url.avif)

- To remove the single point of failures, the web servers, count caches, and database have all been horizontally scaled.
- However, if one web server makes a request to one count cache and another web server makes another request to another count cache, as there is no coordination among the caches there is nothing stopping them returning the same number which would cause a collision. Collisions are something that cannot be tolerated in our system and so a better design is needed.

## Advanced Implementation

![advanced-implementation-tiny-url](./images/advanced-implementation-tiny-url.avif)

### ZooKeeper
- Instead of having distributed count caches, ZooKeeper will be used instead. ZooKeeper can be used as a centralized service for maintaining configuration information and synchronization in a distributed setting.
- In this architecture ZooKeeper will being used to maintain number ranges (1 million values per range). When a web sever comes online it will reach out to the ZooKeeper to get a range. Once given a range a web server will use those numbers to generate short URLs. This will allow us to avoid the collision problem encountered in the previous design.
- Even if a web server were to go down straight after being given a range and those million values were lost, given that we have such a large number of values (3.5 trillion) it isn't really an issue.
- An interviewer could point out that the ZooKeeper is now a single point of failure. So we could horizontally scale instances of the ZooKeeper, but it is also important to remember that unlike the count cache which received a request every time a new URL was to be created. A web server will only make a request to ZooKeeper to get a range of values which reduces the load by a factor of a million (the range chosen).

### Database Selection
- An SQL database like MySQL or PostgreSQL could be a valid choice of database in our design but we may have to introduce sharding to handle the high read volume. However, in this particular case given the high read volume a NoSQL database like Cassandra may be a better choice.An SQL database like MySQL or PostgreSQL could be a valid choice of database in our design but we may have to introduce sharding to handle the high read volume. However, in this particular case given the high read volume a NoSQL database like Cassandra may be a better choice.

### Caching
- If we wanted to further reduce the load on the database we could also introduce a cache where popular short URLs and their corresponding long URLs are stored. Popular choices of caches include Memcached or Redis.
- A cache eviction policy of least recently used (LRU) could be an adequate solution for this system.

## Create URL Flow

![create-flow-tiny-url](./images/create-flow-tiny-url.avif)

1. A client makes a POST request with the long URL in the request parameters. (The load balancer distributes the request to a web server)
2. A web server generates the short URL.
3. A web server saves the long URL and short URL in the database (can also store the URLs in the cache with a specified TTL).
4. The web server responds to the client with the newly created short URL.

## Redirect URL Flow

![redirect-flow-tiny-url](./images/redirect-flow-tiny-url.avif)

1. A client makes a GET request with the short URL. (The load balancer distributes the request to a web server)
2. A web server checks to see if the short URL is stored in the cache.
3. If the short URL is not present in the cache, the web server retrieves the long URL from the database.
4. The web server redirects (301 permanent redirect) the user to the long URL.

## Additional discussion points

1. Analytics
- Counts for each URL to determine which short URLs to cache.
- IP address to store location information to determine where to locate caches etc.

2. Rate Limiting
- Prevent DDoS attacks by malicious users.

3. Security Considerations
- Add random suffix to the short URL to prevent hackers predicting URLs.
- Will give rise to longer URLs but the tradeoff between URL length and security can be worth discussing in the interview.

