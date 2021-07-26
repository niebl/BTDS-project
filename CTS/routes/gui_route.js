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



//TESTAREA
//create hardcoded room schema
/*
var pss1 = new Passage({
    sensorID: "A111.1"
})
var pss2 = new Passage({
    sensorID: "A111.2"
})
var rss1 = new Aoi({
    sensorID: "A111.rss",
    description: "Area of interest: kaffeemaschine",
    observation: {
        time: Date.now(),
        inhabitants: [
            {x: 12.1212, y: 65.1245},
            {x: 10.0032, y: 72.1243}
        ]
    }
})

var room1 = new Room({
    roomNumber : "A111",
    description: "this room is just a test room. two entries, one Aoi",
    passages: [pss1, pss2]
})


try{
    pss1.save()
    pss2.save()
    rss1.save()
    room1.save()
} catch(e){
    console.log(e)
}

console.log(room1)
console.log(pss1)
console.log(pss2)
console.log(rss1)

*/
    
module.exports = router;