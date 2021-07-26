const express = require('express');
const app = express();
const mongoose = require('mongoose');

require('dotenv').config()

mongoose.connect(process.env.MONGO_CONNECTION,{
    useNewUrlParser: true,
    useUnifiedTopology : true,
    useCreateIndex: true
})


const guiRouter = require('./routes/gui_route')

app.set('view engine', 'ejs');
app.use("/", guiRouter);

app.listen(process.env.SERVER_PORT);