const mongoose = require('mongoose');

const PassageSchema = new mongoose.Schema({
    sensorID: {
        type: String,
        index: true,
        required: true,
        unique: true
    }
});

module.exports = mongoose.model('Passage', PassageSchema)