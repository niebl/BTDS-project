const express = require('express');
const app = express();
const mongoose = require('mongoose');

app.use(express.urlencoded({ extended: false }));
require('dotenv').config()

mongoose.connect(process.env.MONGO_CONNECTION,{
    useNewUrlParser: true,
    useUnifiedTopology : true,
    useCreateIndex: true
})


const guiRouter = require('./routes/gui_route')
const configRouter = require('./routes/config_route')

app.set('view engine', 'ejs');
app.use("/", guiRouter);
app.use("/config", configRouter);
app.use(express.static('public'))

app.listen(process.env.SERVER_PORT);