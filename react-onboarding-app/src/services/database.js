// src/services/database.js

import { MongoClient } from 'mongodb';

const uri = process.env.DATABASE_URI; // Database connection string from environment variables
let client;

export const connectToDatabase = async () => {
    if (!client) {
        client = new MongoClient(uri, { useNewUrlParser: true, useUnifiedTopology: true });
        await client.connect();
    }
    return client.db();
};

export const disconnectFromDatabase = async () => {
    if (client) {
        await client.close();
        client = null;
    }
};

export const fetchTables = async (dbName) => {
    const db = await connectToDatabase();
    const collections = await db.listCollections().toArray();
    return collections.map(collection => collection.name);
};