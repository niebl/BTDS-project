const express = require('express');
const router = express.Router();

const Room = require('./../models/room');
const Passage = require('./../models/passage');
const Aoi = require('./../models/aoi');


/**
 * this get-endpoint supplies the webpage overview of the CTS
 */
router.get('/', (req,res) => {
    //TODO: this will be an Object that contains a representation of all the rooms we can view
    const building = null;

    res.render('index');
})


    
module.exports = router;