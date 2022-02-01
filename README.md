# online-library-csharp

Ce repository contient une liste de classes facilitant la création d'envoie de données via TCP en utilisant un modèle client-serveur adapté aux projets de jeux vidéos et autre projets C#.

## Introduction

A ajouter pour pouvoir utiliser les fonctionnalités données.
```csharp
using Online;
```

## Connexion au serveur

D'abord il faut démarrer le serveur avant de pouvoir envoyer quoi que ce soit
```csharp
Server.Instance.Start(
  () => { Console.WriteLine("Succès !") },
  () => { Console.WriteLine("Echec...") }
);
```

Puis il faut connecter le client au serveur
```csharp
// Exemple de connexion en local
ClientOrigin.Instance.Connect(
  "127.0.0.1", Server.Port, "Michel",
  () => { Console.WriteLine("Succès !") },
  () => { Console.WriteLine("Echec...") }
);
```

Et voilà ! 

## Envoi / réception de données

Création d'un packet
```csharp
Packet packet = new Packet(ClientOrigin.Id, SpecialId.Server, "test");
packet.Write("coucou"); // Ecrit une valeur dans le packet
```

Envoi un packet côté client :
```csharp
ClientOrigin.Instance.SendPacket(packet);
```

Envoi un packet côté serveur :
```csharp
Server.Instance.SendPacket(1 /* Id du client destinataire */, packet);
```

Gestion de la réception des données côté client et serveur :
```csharp
// Préparation de l'action à effectuer
AddPacketHandler(
  OnlineSide.Server, // ou OnlineSide.Client
  "test",
  (Packet p) => { /* Actions lors de la réception du paquet du nom indiqué */ }
);
```

## Problèmes

### Problèmes de connexion

Impossible de se connecter ? La raison peut-être l'une des suivantes :
- Un pare-feu peut bloquer les connexions : penser à les paramétrer ou les désactiver;
- Pour initialiser le serveur, il est nécessaire de donner les droits administrateur à l'application (ou au moins le droit de pouvoir écouter les entrées de paquets) ;

### Autres problèmes

- *Sur Unity, les lambdas de succès et d'échec de connexion ne fonctionne pas...* : Unity gère mal les tâches asynchrones, les lambdas ne peuvent faire effet que sur des classes personnelles.
