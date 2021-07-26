const mongoose = require('mongoose');

const AoiSchema = new mongoose.Schema({
    sensorID: {
        type: String,
        index: true,
        required: true,
        unique: true
    },
    description:{
        type: String
    },

    //the observation of the Aoi, as it is updated in regular intervals
    observation:{
        type: Object
    }

})

module.exports = mongoose.model('Aoi', AoiSchema)