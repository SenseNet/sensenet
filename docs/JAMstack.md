## What is **JAMstack**?
  
  
**JAM** stands for JavaScript, API and Markup.

JAMstack is revolutionising the way we think about workflow by providing a simpler developer experience, better performance, lower cost and greater scalability.
This simple guide will help you understand why it exists and how to get started.


   ![JAMstack](https://cdn-media-1.freecodecamp.org/images/uHGkEXe8lXJsmj6cZNQmIW3bpsEzn0mU9Eun)


- **JavaScript**

JS is the first foundation of the whole stack. You can love it or you can hate it but you have to admit that JavaScript is one of the most popular programming languages these days with an incredibly vibrant community. The request and response cycles are based on client side thanks to this technology. You can use either pure JavaScript or use any other framework or library available on the market such as React or Vue.

- **APIs**

Though we did mention the static websites, the next layer of the JAMstack is APIs. Thanks to APIs you can use backend functionality without having a database or the backend engine on your own server. You still have a backend, yes, but deploy only a static website. You can use any API you want, public or private. There are many third-party sites you can choose from. You can also connect to another backend app that you have created.

- **Markup**

The presentation layer of your website. With the JAMstack ecosystem it’s usually a static site generator where templated markup is prebuilt at the build time. You can write your own HTML and CSS code or use a framework such as Hugo, Jekyll or Gatsby, which will greatly improve the time of template development.



### Demo App


``` javascript
//app.js
var buildSite = require('./build-site')
buildSite()
var express = require('express')
var app = express()
app.set('port', process.env.PORT || 3000)
app.use(express.static('build'))
app.get('/rebuild-site', (req, res) => {
  buildSite()
  res.end('Site rebuilt!')
})
app.post('/rebuild-site', (req, res) => {
  buildSite()
  res.end('Site rebuilt!')
})
app.get('*', (req, res) => {
  res.redirect('/404')
})
app.listen(app.get('port') || 3000, () => {
  console.info('==> 🌎  Go to http://localhost:%s', app.get('port'))
})
```




### **WORKFLOW**

Here's how an ideal JAMstack workflow would look:

![JAMstack Workflow](https://miro.medium.com/max/900/1*iaJIWN-1jhRBTiVfmYYdlA.png)



### **BENEFITS**

Here are the main benefits provided by the JAMstack.

- Faster performance **->>** Serve pre-built markup and assets over a CDN

- More secure **->>** No need to worry about server or database vulnerabilities

- Less expensive **->>** Hosting of static files is cheap or even free

- Better developer experience **->>** Front end developers can focus on the front end, without being tied to a monolithic architecture. This usually means quicker and more focused development

- Scalability **->>** If your product suddenly goes viral and has many active users, the CDN seamlessly compensates



### **BEST PRACTICES**

The following tips will help you leverage the best out of the stack.

- Content delivery network **->>** Since all the markup and assets are pre-built, they can be served via CDN. This provides better performance and easier scalability.

- Atomic deploys **->>** Each deploy is a full snapshot of the site. This helps guarantee a consistent version of the site globally.

- Cache invalidation **->>** Once your build is uploaded, the CDN invalidates its cache. This means that your new build is live in an instant.

- Everything in version control **->>** Your codebase lives in a Version Control System, such as Git. The main benefits are: change history of every file, collaborators and traceability.

- Automated builds **->>** Your server is notified when a new build is required, typically via webhooks. The server builds the project, updates the CDNs and the site is live.
