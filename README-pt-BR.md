# Bem-vindo ao sensenet
A primeira Plataforma de Gerenciamento de Conteúdo Corporativo de Código Aberto para .NET! 

> [Experimente no seu ambiente local!](http://www.sensenet.com/try-it)

[![Entre no chat em https://gitter.im/SenseNet/sensenet](https://badges.gitter.im/SenseNet/sensenet.svg)](https://gitter.im/SenseNet/sensenet?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)
[![Status de Compilação](https://dev.azure.com/sensenetplatform/sensenet/_apis/build/status/sensenet)](https://dev.azure.com/sensenetplatform/sensenet/_build/latest?definitionId=1)

> **sensenet Serviços 7.0 estáveis** já disponível! Vá para a seção [Primeiros passos](#GettingStarted) abaixo para começar a experimentar imediatamente!

Se você precisar de...

- um **Repositório de Conteúdo** com um poderoso mecanismo de consulta (construído em [Lucene.Net](https://lucenenet.apache.org)) para armazenar *milhões* de documentos,
- uma **plataforma de desenvolvimento** .Net extensível com muitos recursos que os desenvolvedores irão gostar (*OData REST API* com um cliente .Net SDK, *LINQ para sensenet*, uma *Camada de Conteúdo* unificada - e muito mais),
- uma camada flexível de **segurança** com permissões de conteúdo personalizáveis, respeitadas pelo mecanismo de consulta,
- uma arquitetura corporativa escalável com NLB e gerenciamento de tarefas em segundo plano,
- *workspaces*, *listas* e *bibliotecas* para tornar a colaboração mais fácil

...aqui você encontra o que precisa!

![Workspaces](http://wiki.sensenet.com/images/5/5e/Ws-main.png "Workspaces")

## sensenet pode ser um monte de coisas

- uma plataforma de desenvolvimento
- um portal de internet e intranet
- um repositório de conteúdo central para todos os tipos de conteúdo personalizado
- um ponto de integração entre os seus (ou de seus clientes) aplicativos existentes

Nos informe em qual parte você está mais interessado!

## Licença
sensenet está disponível em duas edições:

1. **Edição da Comunidade**: uma edição suportada pela comunidade [GPL v2](LICENSE) com quase todos os recursos.
   O código fonte está disponível em [CodePlex](http://sensenet.codeplex.com) (para **versão 6.5**) e aqui no *GitHub* (para a nova e componentizada **versão 7.0** - veja detalhes abaixo).
2. **Edição Empresarial**: com recursos adicionais de nível empresarial (como AD sync, provedor MongoDB blob) e suporte ao vendedor! Para detalhes, visite a [página de licenciamento](http://www.sensenet.com/sensenet-ecm/licencing) no nosso site.

## Contato e suporte
Seja você um membro da comunidade ou cliente corporativo. Sinta-se à vontade para visitar nossos canais de comunicação para obter demonstrações, exemplos e suporte:
- Website: http://www.sensenet.com
- Canal de chat principal: https://gitter.im/SenseNet/sensenet
- Todos os canais de chat: https://gitter.im/SenseNet
- Suporte da comunidade: http://stackoverflow.com/questions/tagged/sensenet
- Suporte empresarial: http://support.sensenet.com

<a name="GettingStarted"></a>
## Começando
Atualmente, oferecemos duas versões diferentes do sensenet. Recomendamos a versão 7.0 para novos projetos, pois é mais leve e flexível.

### sensenet 7.0
Uma plataforma ECM moderna que pode ser integrada à aplicativos da web novos ou existentes. Nós modularizamos o sensenet para que você possa instalar apenas as partes que você precisar. Dê uma olhada nos [componentes principais](/docs/sensenet-components.md) atualmente publicados!

Há também vários outros [componentes e plugins](https://github.com/SenseNet/awesome-sensenet) internos e de terceiros, criados nesta plataforma por nós ou pela comunidade.

![componentes sensenet](https://github.com/SenseNet/sn-resources/raw/master/images/sn-components/sn-components.png "componentes sensenet")

- [Componentes principais do sensenet](/docs/sensenet-components.md)
- [Lista incrível de componentes e plugins](https://github.com/SenseNet/awesome-sensenet)
- [Instalação do sensenet para NuGet](/docs/install-sn-from-nuget.md)

#### Depois de instalar o sensenet
Depois que você instalar o [sensenet](/docs/install-sn-from-nuget.md), você pode iniciar enviando solicitações para o site. 

Considere utilizar os seguintes projetos do cliente para manipular dados no Repositório de Conteúdo por meio de sua REST API:

- [Cliente sensenet JavaScript](https://github.com/SenseNet/sn-client-js)
- [Cliente sensenet  .Net ](https://github.com/SenseNet/sn-client-dotnet)

Para exemplos detalhados do lado do cliente, por favor visite o [artigo REST API](http://wiki.sensenet.com/OData_REST_API).

### sensenet 6.5
Um Corporativo CMS rico em recursos com interface do usuário predefinida e blocos de construção: páginas, portlets, controles de ação e muito mais. Crie sua solução com quase nenhum esforço de desenvolvimento.

Se você é novo em sensenet, vale a pena conferir estes artigos introdutórios em nossa [wiki](http://wiki.sensenet.com):
- [Começando - usando sensenet](http://wiki.sensenet.com/Getting_started_-_using_Sense/Net)
- [Começando - instalação e manutenção](http://wiki.sensenet.com/Getting_started_-_installation_and_maintenance)
- [Começando - construindo portais](http://wiki.sensenet.com/Getting_started_-_building_portals)
- [Começando - desenvolvendo aplicações](http://wiki.sensenet.com/Getting_started_-_developing_applications)

## Contribuição
Todas as formas de contribuições são bem-vindas! Ficamos felizes se você tiver uma idéia, correção de bug ou solicitação de recurso para compartilhar com outras pessoas. Por favor consulte nosso [Guia de Contribuição](CONTRIBUTING.md) para obter detalhes.

*Este artigo foi traduzido do [Inglês](README.md) para [Português (Brasil)](README-pt-BR.md).*
